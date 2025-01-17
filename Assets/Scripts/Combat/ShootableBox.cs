using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootableBox : Shootable
{
    public int Health = 3;
    public void Awake(){
        this.currentHealth = Health;
    }
    public override void Damage(int damageAmount)
    {
        //subtract damage amount when Damage function is called
        currentHealth -= damageAmount;

        //Check if health has fallen below zero
        if (currentHealth <= 0)
        {
            //if health has fallen below zero, deactivate it 
            gameObject.SetActive(false);
        }
    }
}
