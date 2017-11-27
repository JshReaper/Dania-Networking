using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;

public class Health : MonoBehaviour
{

	public const int maxHealth = 100;
	public bool destroyOnDeath;

	
	public int currentHealth = maxHealth;

	public RectTransform healthBar;


	private void Update()
	{
		healthBar.sizeDelta = new Vector2(currentHealth, healthBar.sizeDelta.y);
	}

	public void TakeDamage(int amount)
	{
	

		currentHealth -= amount;
		if (currentHealth <= 0)
		{
			if (destroyOnDeath)
			{
				Destroy(gameObject);
			}
			else
			{
				currentHealth = maxHealth;
			}
		}

		
	}

	void OnChangeHealth(int currentHealth)
	{
		
	}

	
}