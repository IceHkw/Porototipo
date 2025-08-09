using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

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
    private OverDriveDisplay overDriveDisplay; // <- NUEVO

    // ===== SECCIÓN PARA KILLSTREAKS =====
    private List<KillStreakSlotUI> killStreakSlotsUI = new List<KillStreakSlotUI>();
    private VisualElement killStreakContainer;
    // ===========================================

    // Referencias del jugador y otros sistemas
    private PlayerStats playerStats;
    private PlayerController playerController;
    private KillStreakManager killStreakManager;
    private OverDriveManager overDriveManager; // <- NUEVO

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
        overDriveDisplay = new OverDriveDisplay(hudRoot); // <- NUEVO
        movementAbilityDisplay = new AbilityDisplay(hudRoot, "movement-ability");
        ultimateAbilityDisplay = new AbilityDisplay(hudRoot, "ultimate-ability");
        comboDisplay = new ComboDisplay(hudRoot);

        movementAbilityDisplay?.HideOverlay();
        ultimateAbilityDisplay?.HideOverlay();

        // ===== INICIALIZACIÓN DE KILLSTREAK UI =====
        killStreakContainer = hudRoot?.Q<VisualElement>("killstreak-container");
        if (killStreakContainer != null)
        {
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

    public ComboDisplay GetComboDisplay()
    {
        return comboDisplay;
    }

    public void ConnectToPlayer(PlayerStats stats, PlayerController controller)
    {
        DisconnectFromPlayer();

        playerStats = stats;
        playerController = controller;

        if (playerStats != null)
        {
            playerStats.OnHealthChanged += UpdateHealth;
            UpdateHealth(playerStats.CurrentHealth);
        }

        if (playerController != null)
        {
            UpdateAbilities();
            SubscribeToAbilityEvents();
        }
    }

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
            UpdateKillStreakSlots(killStreakManager.Slots);
        }
    }

    // ===== MÉTODO NUEVO PARA OVERDRIVE =====
    public void ConnectToOverDriveManager(OverDriveManager manager)
    {
        if (overDriveManager != null)
        {
            overDriveManager.OnChargeChanged -= HandleOverDriveChargeChanged;
            overDriveManager.OnLevelChanged -= HandleOverDriveLevelChanged;
        }

        overDriveManager = manager;

        if (overDriveManager != null)
        {
            overDriveManager.OnChargeChanged += HandleOverDriveChargeChanged;
            overDriveManager.OnLevelChanged += HandleOverDriveLevelChanged;

            // Actualización inicial con valores correctos
            HandleOverDriveChargeChanged(0f, overDriveManager.CurrentMaxCharge);
            HandleOverDriveLevelChanged(overDriveManager.CurrentLevel, overDriveManager.MaxLevel);
        }
    }
    // =======================================

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

    // ===== HANDLER NUEVO PARA OVERDRIVE =====
    private void HandleOverDriveChargeChanged(float current, float max)
    {
        overDriveDisplay?.UpdateDisplay(current, max);
    }

    // ========================================

    private void HandleOverDriveLevelChanged(int currentLevel, int maxLevel)
    {
        overDriveDisplay?.UpdateLevel(currentLevel, maxLevel);

        // Opcionalmente, mostrar efectos visuales cuando sube de nivel
        if (currentLevel > 0)
        {
            Debug.Log($"[HUD] OverDrive Level Up: {currentLevel}/{maxLevel}");
        }
    }

    public void Show() => container.style.display = DisplayStyle.Flex;
    public void Hide() => container.style.display = DisplayStyle.None;
    public void RefreshUI()
    {
        if (playerStats != null) UpdateHealth(playerStats.CurrentHealth);
        UpdateAbilities();
        if (killStreakManager != null) UpdateKillStreakSlots(killStreakManager.Slots);
        if (overDriveManager != null) HandleOverDriveChargeChanged(overDriveManager.ChargePercent, 1f);
    }
    public void Cleanup()
    {
        DisconnectFromPlayer();
        if (killStreakManager != null) killStreakManager.OnSlotsChanged -= UpdateKillStreakSlots;
        if (overDriveManager != null) overDriveManager.OnChargeChanged -= HandleOverDriveChargeChanged;
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
        comboContainer = root?.Q<VisualElement>("combo-display");
        comboValue = root?.Q<Label>("combo-value");

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

    public void SetScale(float scale)
    {
        if (comboContainer != null)
        {
            comboContainer.transform.scale = new Vector3(scale, scale, 1f);
        }
    }
}

// ===== CLASE PARA LOS SLOTS DE KILLSTREAK =====
public class KillStreakSlotUI
{
    private VisualElement slotRoot;
    private VisualElement icon;
    private Label infoText;
    private Label nameText;
    private VisualElement activeOverlay;

    public KillStreakSlotUI(VisualElement rootElement)
    {
        slotRoot = rootElement;
        icon = slotRoot.Q<VisualElement>("ks-icon");
        nameText = slotRoot.Q<Label>("killstreak-name");
        infoText = slotRoot.Q<Label>("killstreak-combo");
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

        if (nameText != null)
        {
            nameText.text = slot.definition.KillStreakName;
        }

        if (infoText != null)
        {
            if (slot.isActive)
            {
                infoText.text = $"NIVEL {slot.currentLevel}";
            }
            else
            {
                infoText.text = $"{slot.GetRequiredCombo()}K";
            }
        }

        if (activeOverlay != null)
        {
            activeOverlay.style.display = slot.isActive ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}

// ===== NUEVA CLASE PARA EL DISPLAY DE OVERDRIVE =====
public class OverDriveDisplay
{
    private VisualElement fillElement;
    private Label levelText;

    public OverDriveDisplay(VisualElement root)
    {
        // Buscar primero el contenedor, luego el elemento fill
        var container = root?.Q<VisualElement>("overdrive-container");
        if (container != null)
        {
            fillElement = container.Q<VisualElement>("overdrive-fill");

            // Opcionalmente, podríamos agregar un texto para mostrar el nivel
            var barContainer = container.Q<VisualElement>("overdrive-bar");
            if (barContainer != null)
            {
                levelText = new Label("LVL 0");
                levelText.AddToClassList("overdrive-level-text");
                levelText.style.position = Position.Absolute;
                levelText.style.right = 5;
                levelText.style.top = 0;
                levelText.style.color = Color.white;
                levelText.style.fontSize = 10;
                barContainer.Add(levelText);
            }
        }
    }

    public void UpdateDisplay(float current, float max)
    {
        if (fillElement != null && max > 0)
        {
            // Calcular el porcentaje correctamente (sin multiplicar por 100)
            float percent = (current / max) * 100f;
            fillElement.style.width = Length.Percent(percent);

            // Efecto de color gradual según el porcentaje
            if (percent > 75)
            {
                fillElement.style.backgroundColor = new Color(1f, 0.2f, 0.2f); // Rojo
            }
            else if (percent > 50)
            {
                fillElement.style.backgroundColor = new Color(1f, 0.65f, 0f); // Naranja
            }
            else if (percent > 25)
            {
                fillElement.style.backgroundColor = new Color(1f, 1f, 0f); // Amarillo
            }
            else
            {
                fillElement.style.backgroundColor = new Color(1f, 0.65f, 0f); // Naranja base
            }
        }
    }

    public void UpdateLevel(int currentLevel, int maxLevel)
    {
        if (levelText != null)
        {
            levelText.text = $"LVL {currentLevel}";
        }
    }
}