using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class HUDController : IUIController
{
    // Referencias a elementos UI
    private VisualElement container;
    private VisualElement hudRoot;

    // Elementos del HUD
    private HealthDisplay healthDisplay;
    private AbilityDisplay movementAbilityDisplay;
    private AbilityDisplay ultimateAbilityDisplay;
    private ComboDisplay comboDisplay;

    // ===== SECCIÓN AÑADIDA PARA KILLSTREAKS =====
    private List<KillStreakSlotUI> killStreakSlotsUI = new List<KillStreakSlotUI>();
    private VisualElement killStreakContainer;
    // ===========================================

    // Referencias del jugador y otros sistemas
    private PlayerStats playerStats;
    private PlayerController playerController;
    private KillStreakManager killStreakManager;

    public void Initialize(VisualElement container, VisualTreeAsset template)
    {
        this.container = container;
        if (template != null)
        {
            template.CloneTree(container);
            hudRoot = container.Q<VisualElement>("hud-root");
        }
        InitializeDisplays();
    }

    void InitializeDisplays()
    {
        // Inicializar displays existentes
        healthDisplay = new HealthDisplay(hudRoot);
        movementAbilityDisplay = new AbilityDisplay(hudRoot, "movement-ability");
        ultimateAbilityDisplay = new AbilityDisplay(hudRoot, "ultimate-ability");
        comboDisplay = new ComboDisplay(hudRoot);

        movementAbilityDisplay?.HideOverlay();
        ultimateAbilityDisplay?.HideOverlay();

        // ===== INICIALIZACIÓN DE KILLSTREAK UI =====
        killStreakContainer = hudRoot?.Q<VisualElement>("killstreak-container");
        if (killStreakContainer != null)
        {
            // Se asume que en el UXML hay 3 elementos con los nombres killstreak-slot-0, 1 y 2
            for (int i = 0; i < 3; i++)
            {
                var slotElement = killStreakContainer.Q<VisualElement>($"killstreak-slot-{i}");
                if (slotElement != null)
                {
                    killStreakSlotsUI.Add(new KillStreakSlotUI(slotElement));
                }
            }
        }
        // ===========================================
    }

    // --- MÉTODO CORREGIDO/AÑADIDO ---
    // Permite que el ComboManager acceda al display para animarlo.
    public ComboDisplay GetComboDisplay()
    {
        return comboDisplay;
    }
    // --------------------------------

    public void ConnectToPlayer(PlayerStats stats, PlayerController controller)
    {
        DisconnectFromPlayer(); // Limpiar suscripciones anteriores

        playerStats = stats;
        playerController = controller;

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealth;
            UpdateHealth(playerStats.CurrentHealth); // Actualización inicial
        }

        if (playerController != null)
        {
            UpdateAbilities();
            SubscribeToAbilityEvents();
        }
    }

    // ===== MÉTODO AÑADIDO PARA KILLSTREAKS =====
    public void ConnectToKillStreakManager(KillStreakManager manager)
    {
        if (killStreakManager != null)
        {
            killStreakManager.OnSlotsChanged -= UpdateKillStreakSlots;
        }

        killStreakManager = manager;

        if (killStreakManager != null)
        {
            killStreakManager.OnSlotsChanged += UpdateKillStreakSlots;
            UpdateKillStreakSlots(killStreakManager.Slots); // Actualización inicial
        }
    }
    // ===========================================

    private void DisconnectFromPlayer()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged -= UpdateHealth;
        }

        if (playerController != null)
        {
            if (playerController.HasMovementAbility)
                playerController.MovementAbility.OnCooldownChanged -= HandleMovementCooldownChanged;

            if (playerController.HasUltimateAbility)
            {
                playerController.UltimateAbility.OnChargeChanged -= HandleUltimateChargeChanged;
                playerController.UltimateAbility.OnActiveStateChanged -= HandleUltimateActiveStateChanged;
            }
        }
    }

    void UpdateHealth(int currentHealth)
    {
        if (playerStats != null)
        {
            healthDisplay?.UpdateHealth(currentHealth, playerStats.MaxHealth);
        }
    }

    void UpdateAbilities()
    {
        if (playerController == null) return;

        if (playerController.HasMovementAbility)
        {
            var ability = playerController.MovementAbility;
            movementAbilityDisplay?.SetAbility(ability.AbilityName, ability.AbilityIcon);
            float cooldownPercent = ability.MaxCooldown > 0 ? ability.CurrentCooldown / ability.MaxCooldown : 0f;
            movementAbilityDisplay?.UpdateCooldown(cooldownPercent, ability.CurrentCooldown);
        }

        if (playerController.HasUltimateAbility)
        {
            var ultimate = playerController.UltimateAbility;
            ultimateAbilityDisplay?.SetAbility(ultimate.AbilityName, ultimate.AbilityIcon);
            ultimateAbilityDisplay?.UpdateCharge(ultimate.ChargePercent);
        }
    }

    // ===== MÉTODO AÑADIDO PARA KILLSTREAKS =====
    void UpdateKillStreakSlots(KillStreakSlot[] slots)
    {
        for (int i = 0; i < killStreakSlotsUI.Count; i++)
        {
            if (i < slots.Length)
            {
                killStreakSlotsUI[i].UpdateSlot(slots[i]);
            }
        }
    }
    // ===========================================

    void SubscribeToAbilityEvents()
    {
        if (playerController == null) return;

        if (playerController.HasMovementAbility)
        {
            playerController.MovementAbility.OnCooldownChanged += HandleMovementCooldownChanged;
        }

        if (playerController.HasUltimateAbility)
        {
            playerController.UltimateAbility.OnChargeChanged += HandleUltimateChargeChanged;
            playerController.UltimateAbility.OnActiveStateChanged += HandleUltimateActiveStateChanged;
        }
    }

    // --- Handlers de eventos (sin cambios) ---
    void HandleMovementCooldownChanged(float currentCooldown, float maxCooldown, float cooldownPercent)
    {
        movementAbilityDisplay?.UpdateCooldown(cooldownPercent, currentCooldown);
    }

    void HandleUltimateChargeChanged(float chargePercent)
    {
        if (playerController?.UltimateAbility != null && !playerController.UltimateAbility.IsActive)
        {
            ultimateAbilityDisplay?.UpdateCharge(chargePercent);
        }
    }

    void HandleUltimateActiveStateChanged(bool isActive, float remainingTime)
    {
        if (isActive)
        {
            ultimateAbilityDisplay?.ShowActiveState();
        }
        else
        {
            if (playerController?.UltimateAbility != null)
            {
                ultimateAbilityDisplay?.UpdateCharge(playerController.UltimateAbility.ChargePercent);
            }
        }
    }

    public void Show() => container.style.display = DisplayStyle.Flex;
    public void Hide() => container.style.display = DisplayStyle.None;
    public void RefreshUI()
    {
        if (playerStats != null) UpdateHealth(playerStats.CurrentHealth);
        UpdateAbilities();
        if (killStreakManager != null) UpdateKillStreakSlots(killStreakManager.Slots);
    }
    public void Cleanup()
    {
        DisconnectFromPlayer();
        if (killStreakManager != null) killStreakManager.OnSlotsChanged -= UpdateKillStreakSlots;
    }
}

// =======================================================
// ===== CLASES DE DISPLAY AUXILIARES (CON CORRECCIONES) =====
// =======================================================

public class HealthDisplay
{
    private VisualElement healthFill;
    private Label healthText;

    public HealthDisplay(VisualElement root)
    {
        healthFill = root?.Q<VisualElement>("health-fill");
        healthText = root?.Q<Label>("health-text");
    }

    public void UpdateHealth(int current, int max)
    {
        if (healthFill != null)
        {
            float percentage = max > 0 ? (float)current / max : 0f;
            healthFill.style.width = Length.Percent(percentage * 100);
        }
        if (healthText != null)
        {
            healthText.text = $"{current}/{max}";
        }
    }
}

public class AbilityDisplay
{
    private VisualElement icon;
    private VisualElement cooldownOverlay;
    private Label cooldownText;

    public AbilityDisplay(VisualElement root, string abilityName)
    {
        var abilityElement = root?.Q<VisualElement>(abilityName);
        if (abilityElement != null)
        {
            icon = abilityElement.Q<VisualElement>(abilityName + "-icon");
            cooldownOverlay = abilityElement.Q<VisualElement>(abilityName + "-cooldown");
            cooldownText = abilityElement.Q<Label>(abilityName + "-cooldown-text");
        }
    }
    public void SetAbility(string name, Sprite iconSprite)
    {
        if (icon != null && iconSprite != null)
        {
            icon.style.backgroundImage = new StyleBackground(iconSprite);
        }
    }
    public void HideOverlay()
    {
        if (cooldownOverlay != null) cooldownOverlay.style.display = DisplayStyle.None;
    }
    public void UpdateCooldown(float cooldownPercent, float cooldownTime)
    {
        if (cooldownOverlay != null)
        {
            bool hasCooldown = cooldownPercent > 0;
            cooldownOverlay.style.display = hasCooldown ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasCooldown) cooldownOverlay.style.height = Length.Percent(cooldownPercent * 100);
        }
        if (cooldownText != null)
        {
            cooldownText.text = cooldownTime > 0.1f ? Mathf.CeilToInt(cooldownTime).ToString() : "";
        }
    }
    public void UpdateCharge(float chargePercent)
    {
        if (cooldownOverlay != null)
        {
            bool isCharging = chargePercent < 1f;
            cooldownOverlay.style.display = isCharging ? DisplayStyle.Flex : DisplayStyle.None;
            if (isCharging) cooldownOverlay.style.height = Length.Percent((1f - chargePercent) * 100);
        }
        if (cooldownText != null)
        {
            cooldownText.text = chargePercent < 1f ? $"{Mathf.FloorToInt(chargePercent * 100)}%" : "READY";
        }
    }
    public void ShowActiveState()
    {
        if (cooldownOverlay != null) cooldownOverlay.style.display = DisplayStyle.None;
        if (cooldownText != null) cooldownText.text = "ACTIVE";
    }
}

public class ComboDisplay
{
    private VisualElement comboContainer;
    private Label comboValue;

    public ComboDisplay(VisualElement root)
    {
        // Se asume que en el UXML existe un elemento "combo-display" que contiene al label
        comboContainer = root?.Q<VisualElement>("combo-display");
        comboValue = root?.Q<Label>("combo-value");

        // Si no se encuentra un contenedor específico, se usará el propio label para la animación
        if (comboContainer == null)
        {
            comboContainer = comboValue;
        }
    }

    public void UpdateCombo(int combo)
    {
        if (comboValue == null) return;
        comboValue.text = $"x{combo}";
    }

    // Requerido por ComboManager para la animación de pulso.
    public void SetScale(float scale)
    {
        if (comboContainer != null)
        {
            comboContainer.transform.scale = new Vector3(scale, scale, 1f);
        }
    }
}

// ===== NUEVA CLASE PARA LOS SLOTS DE KILLSTREAK =====
public class KillStreakSlotUI
{
    private VisualElement slotRoot;
    private VisualElement icon;
    private Label infoText;
    private VisualElement activeOverlay;

    public KillStreakSlotUI(VisualElement rootElement)
    {
        slotRoot = rootElement;
        icon = slotRoot.Q<VisualElement>("ks-icon");
        infoText = slotRoot.Q<Label>("ks-info");
        activeOverlay = slotRoot.Q<VisualElement>("ks-active-overlay");
    }

    public void UpdateSlot(KillStreakSlot slot)
    {
        if (slot == null || !slot.IsValid())
        {
            slotRoot.style.display = DisplayStyle.None;
            return;
        }

        slotRoot.style.display = DisplayStyle.Flex;

        if (icon != null && slot.definition.Icon != null)
        {
            icon.style.backgroundImage = new StyleBackground(slot.definition.Icon);
        }

        if (infoText != null)
        {
            infoText.text = $"{slot.GetRequiredCombo()}K";
        }

        if (activeOverlay != null)
        {
            activeOverlay.style.display = slot.isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}