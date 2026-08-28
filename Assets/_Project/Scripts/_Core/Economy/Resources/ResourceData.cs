using System;
using UnityEngine;

namespace ER
{
    namespace Resources
    {
        [System.Serializable]
        public class ResourceData
        {
            public bool IsActive = true;
            
            public float Amount = 0f;

            public ResourceData(){}

            public ResourceData(bool initialActive = true, float initialAmount = 0f)
            {
                IsActive = initialActive;

                Amount = initialAmount;
            }

            public void AddAmount(float amount)
            {
                Amount += amount;
            }

            public bool IsResourceActive()
            {
                return IsActive;
            }

            public float GetAmount()
            {
                return Amount;
            }

            public override string ToString()
            {
                return $"Amount: {Amount:F1}%, Active: {IsActive}";
            }
        }
    }
}