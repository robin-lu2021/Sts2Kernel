"""
BinaryPipe client for sts2kernel.

Connects to the kernel via Windows named pipe using the binary protocol,
and provides a command-line interface for the user.

Protocol: length-prefixed frames
  [4 bytes LE: msgType] [4 bytes LE: payloadLen] [payloadLen bytes: UTF-8]

Kernel → Client:
  0x01 TextOutput    text line
  0x02 PromptInput   prompt, wait for InputResponse
  0x03 ShowChoices   "key0|text0\nkey1|text1\n..."
  0x04 Exit

Client → Kernel:
  0x10 InputResponse  user text
  0x11 ChoiceResponse  0-based index as decimal string
"""

import ctypes
import ctypes.wintypes as wt
import struct
import sys

# ── Win32 constants & functions ────────────────────────────────

GENERIC_READ    = 0x80000000
GENERIC_WRITE   = 0x40000000
OPEN_EXISTING   = 3
INVALID_HANDLE_VALUE = ctypes.c_void_p(-1).value

_kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)

_CreateFileW = _kernel32.CreateFileW
_CreateFileW.argtypes = [
    wt.LPCWSTR, wt.DWORD, wt.DWORD, wt.LPVOID,
    wt.DWORD, wt.DWORD, wt.HANDLE
]
_CreateFileW.restype = wt.HANDLE

_ReadFile = _kernel32.ReadFile
_ReadFile.argtypes = [
    wt.HANDLE, wt.LPVOID, wt.DWORD,
    ctypes.POINTER(wt.DWORD), wt.LPVOID
]
_ReadFile.restype = wt.BOOL

_WriteFile = _kernel32.WriteFile
_WriteFile.argtypes = [
    wt.HANDLE, wt.LPCVOID, wt.DWORD,
    ctypes.POINTER(wt.DWORD), wt.LPVOID
]
_WriteFile.restype = wt.BOOL

_CloseHandle = _kernel32.CloseHandle
_CloseHandle.argtypes = [wt.HANDLE]
_CloseHandle.restype = wt.BOOL


# ── Message type constants ─────────────────────────────────────

MSG_TEXT_OUTPUT   = 0x01
MSG_PROMPT_INPUT  = 0x02
MSG_SHOW_CHOICES  = 0x03
MSG_EXIT          = 0x04

MSG_INPUT_RESPONSE  = 0x10
MSG_CHOICE_RESPONSE = 0x11


# ── Pipe client ────────────────────────────────────────────────

class PipeClient:
    """Thin wrapper around a Windows named pipe with binary framing."""

    HEADER_SIZE = 8  # 4 msgType + 4 payloadLen

    def __init__(self, pipe_path: str):
        self._path = pipe_path
        self._handle: int | None = None

    def connect(self, timeout_ms: int = 5000) -> None:
        """Open the named pipe. Raises on failure."""
        handle = _CreateFileW(
            self._path,
            GENERIC_READ | GENERIC_WRITE,
            0, None, OPEN_EXISTING, 0, None
        )
        if handle == INVALID_HANDLE_VALUE:
            err = ctypes.get_last_error()
            raise OSError(f"CreateFile failed for {self._path} (error {err})")
        self._handle = handle

    # ── low-level I/O ──────────────────────────────────────────

    def _read_exact(self, size: int) -> bytes:
        buf = ctypes.create_string_buffer(size)
        total = 0
        while total < size:
            n = wt.DWORD(0)
            ok = _ReadFile(
                self._handle,
                ctypes.byref(buf, total),
                size - total,
                ctypes.byref(n),
                None
            )
            if not ok or n.value == 0:
                raise ConnectionError("Pipe read failed or closed.")
            total += n.value
        return buf.raw

    def _write_all(self, data: bytes) -> None:
        n = wt.DWORD(0)
        ok = _WriteFile(self._handle, data, len(data), ctypes.byref(n), None)
        if not ok or n.value != len(data):
            raise ConnectionError("Pipe write failed.")

    def read_message(self) -> tuple[int, bytes]:
        header = self._read_exact(self.HEADER_SIZE)
        msg_type, payload_len = struct.unpack("<ii", header)
        payload = self._read_exact(payload_len) if payload_len > 0 else b""
        return msg_type, payload

    def write_message(self, msg_type: int, payload: bytes) -> None:
        header = struct.pack("<ii", msg_type, len(payload))
        self._write_all(header + payload)

    # ── typed sends ────────────────────────────────────────────

    def send_input_response(self, text: str) -> None:
        self.write_message(MSG_INPUT_RESPONSE, text.encode("utf-8"))

    def send_choice_response(self, zero_based_index: int) -> None:
        self.write_message(
            MSG_CHOICE_RESPONSE,
            str(zero_based_index).encode("utf-8")
        )

    # ── lifecycle ──────────────────────────────────────────────

    def close(self) -> None:
        if self._handle is not None:
            _CloseHandle(self._handle)
            self._handle = None


# ── Main loop ──────────────────────────────────────────────────

def run(pipe_name: str = "sts2kernel") -> None:
    pipe_path = rf"\\.\pipe\{pipe_name}"
    print(f"Connecting to {pipe_path} ...")
    client = PipeClient(pipe_path)
    client.connect()
    print("Connected.\n")

    try:
        while True:
            msg_type, payload = client.read_message()
            text = payload.decode("utf-8") if payload else ""

            if msg_type == MSG_TEXT_OUTPUT:
                print(text)

            elif msg_type == MSG_PROMPT_INPUT:
                user_input = input(text)
                client.send_input_response(user_input)

            elif msg_type == MSG_SHOW_CHOICES:
                # Display choices for user reference
                if text:
                    for line in text.split("\n"):
                        parts = line.split("|", 1)
                        if len(parts) == 2:
                            print(f"  {parts[0]}: {parts[1]}")
                        else:
                            print(f"  {line}")

            elif msg_type == MSG_EXIT:
                print("[Kernel exited.]")
                break

            else:
                print(f"[Unknown message type: 0x{msg_type:02X}]")

    except (ConnectionError, OSError, KeyboardInterrupt) as e:
        print(f"\n[Disconnected: {e}]")
    finally:
        client.close()


# ── Entry point ────────────────────────────────────────────────

if __name__ == "__main__":
    import argparse
    parser = argparse.ArgumentParser(description="BinaryPipe client for sts2kernel")
    parser.add_argument("--pipe-name", default="sts2kernel", help="Named pipe name (default: sts2kernel)")
    args = parser.parse_args()
    run(pipe_name=args.pipe_name)
