// HealthComponent.cs (continued)
using System;
using AhBearStudios.Core.DependencyInjection.Attributes;
using AhBearStudios.Core.Messaging;
using AhBearStudios.Core.Messaging.Extensions;
using AhBearStudios.Core.Messaging.Interfaces;
using UnityEngine;
using VContainer;

namespace AhBearStudios.Core.Messaging.Examples
{
    /// <summary>
    /// Sample component that publishes messages.
    /// </summary>
    public class HealthComponent : MonoBehaviour
    {
        [Inject] private IMessageBus _messageBus;
        
        [SerializeField] private string _playerId;
        [SerializeField] private float _maxHealth = 100f;
        
        private float _currentHealth;
        
        private void Start()
        {
            _currentHealth = _maxHealth;
        }
        
        /// <summary>
        /// Takes damage and updates health.
        /// </summary>
        /// <param name="amount">The amount of damage to take.</param>
        public void TakeDamage(float amount)
        {
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            
            // Publish a message about the health change
            _messageBus.Publish(new PlayerHealthChangedMessage
            {
                PlayerId = _playerId,
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth
            });
        }
        
        /// <summary>
        /// Heals the player.
        /// </summary>
        /// <param name="amount">The amount of health to restore.</param>
        public void Heal(float amount)
        {
            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
            
            // Publish a message about the health change
            _messageBus.Publish(new PlayerHealthChangedMessage
            {
                PlayerId = _playerId,
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth
            });
        }
    }
}