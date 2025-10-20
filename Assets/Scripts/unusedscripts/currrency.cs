using UnityEngine;

public class CurrencySystem : MonoBehaviour
{
	[SerializeField]
	private int startingBalance = 100;

	[SerializeField]
	private int prescriptionReward = 20;

	[SerializeField]
	private int deliveryReward = 50;

	private int currentBalance;

	public System.Action<int> OnBalanceChanged;

	void Awake()
	{
		currentBalance = Mathf.Max(0, startingBalance);
	}

	public int GetBalance()
	{
		return currentBalance;
	}

	public bool CanAfford(int amount)
	{
		return amount <= currentBalance;
	}

	public bool Spend(int amount)
	{
		if (amount <= 0) return true;
		if (!CanAfford(amount)) return false;
		currentBalance -= amount;
		OnBalanceChanged?.Invoke(currentBalance);
		return true;
	}

	public void Earn(int amount)
	{
		if (amount <= 0) return;
		currentBalance += amount;
		OnBalanceChanged?.Invoke(currentBalance);
	}

	public void AwardForPrescription()
	{
		Earn(prescriptionReward);
	}

	public void AwardForDelivery()
	{
		Earn(deliveryReward);
	}
}


