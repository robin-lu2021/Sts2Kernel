namespace MegaCrit.Sts2.Core.Entities.Potions;

public enum PotionProcureFailureReason
{
	None,
	TooFull,
	NotAllowed
}

public class PotionProcureResult
{
	public bool success;

	public PotionModel potion = null!;

	public PotionProcureFailureReason failureReason;
}
