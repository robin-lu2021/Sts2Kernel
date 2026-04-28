namespace MegaCrit.Sts2.Core.ControllerInput;

public static class MegaInput
{
	public const string up = "ui_up";

	public const string down = "ui_down";

	public const string left = "ui_left";

	public const string right = "ui_right";

	public const string accept = "ui_accept";

	public const string select = "ui_select";

	public const string cancel = "ui_cancel";

	public const string selectCard1 = "mega_select_card_1";

	public const string selectCard2 = "mega_select_card_2";

	public const string selectCard3 = "mega_select_card_3";

	public const string selectCard4 = "mega_select_card_4";

	public const string selectCard5 = "mega_select_card_5";

	public const string selectCard6 = "mega_select_card_6";

	public const string selectCard7 = "mega_select_card_7";

	public const string selectCard8 = "mega_select_card_8";

	public const string selectCard9 = "mega_select_card_9";

	public const string selectCard10 = "mega_select_card_10";

	public const string releaseCard = "mega_release_card";

	public const string topPanel = "mega_top_panel";

	public const string viewDrawPile = "mega_view_draw_pile";

	public const string viewDiscardPile = "mega_view_discard_pile";

	public const string viewDeckAndTabLeft = "mega_view_deck_and_tab_left";

	public const string viewExhaustPileAndTabRight = "mega_view_exhaust_pile_and_tab_right";

	public const string viewMap = "mega_view_map";

	public const string pauseAndBack = "mega_pause_and_back";

	public const string back = "mega_back";

	public const string peek = "mega_peek";

	public static string[] AllInputs => new string[15]
	{
		accept, cancel, down, left, pauseAndBack, peek, right, select, topPanel, up,
		viewDeckAndTabLeft, viewDiscardPile, viewDrawPile, viewExhaustPileAndTabRight, viewMap
	};
}
