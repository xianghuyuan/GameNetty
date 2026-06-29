using System.Collections.Generic;
using ET;
using UnityEngine;

namespace GameLogic
{
    public partial class BattleMainWindow : UIWindow
    {
        private void OnClickBookmarkBtn()
        {
            ToggleBuffPanel();
        }

        private void OnClickPauseBtn()
        {
            DeleteLastEmitter();
        }

        private partial void OnClickGearBtn()
        {
            ToggleEmitterPanel();
        }

        private const int MaxDebugEmitterSlots = 8;
        private const int DefaultDebugBuffGroupId = 61021;
        private const int MonsterDeathRewardOptionCount = 3;

        private int? _currentControlMode;
        private UIText _tmpBossName;
        private UIText _tmpBossHp;
        private UIText _tmpWave;
        private UIText _tmpPlayerHp;
        private UIText _tmpControlMode;
        private UIText _tmpEmitterInfo;
        private RectTransform _tfEmitterBuffRoot;
        private GameObject _gobuffTemplate;

        
        private Battle _battle;
        

        private long _playerUnitId;
        private long _bossUnitId;
        private BattleEmitterAddPanelWidget _emitterAddPanelWidget;
        private BattleBuffAddPanelWidget _buffAddPanelWidget;
        private BattleGMWidget _gmWidget;
        private long _selectedEmitterVehicleId;

        protected override void OnCreate()
        {
            CacheLayoutRefs();
            CreateEmitterAddPanelWidget();
            CreateBuffAddPanelWidget();
            CreateGMWidget();
            ResetBattleData();
            BattleUIHelper.BindMainWindow(this);
        }

        protected override void OnDestroy()
        {
            BattleUIHelper.BindMainWindow(null);
            _battle = null;
            _emitterAddPanelWidget = null;
            _buffAddPanelWidget = null;
            _gmWidget = null;
            _selectedEmitterVehicleId = 0;
            _playerUnitId = 0;
            _bossUnitId = 0;
        }

        protected override void OnUpdate()
        {
            RefreshEmitterOwnedBarWidget();

            if (_emitterAddPanelWidget != null && _emitterAddPanelWidget.Visible)
            {
                _emitterAddPanelWidget.Refresh(_battle, _playerUnitId);
            }

            if (_buffAddPanelWidget != null && _buffAddPanelWidget.Visible)
            {
                _buffAddPanelWidget.Refresh();
            }

            RefreshDebugButtonStates();

            _hasOverrideUpdate = true;
        }

        public void SetBattle(Battle battle)
        {
            ResetBattleData();
            _battle = battle;
            if (battle == null)
            {
                return;
            }

            foreach (BattleUnit unit in battle.GetAllBattleUnits())
            {
                if (_playerUnitId == 0 && unit.Camp == UnitCamp.Friend)
                {
                    _playerUnitId = unit.Id;
                    RefreshPlayer(unit);
                }

                if (_bossUnitId == 0 && unit.IsBoss)
                {
                    _bossUnitId = unit.Id;
                    RefreshBoss(unit);
                }
            }

            SetWave(battle.CurrentWave, battle.TotalWaves);
            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
        }

        public void RefreshUnit(BattleUnit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            if (unit.Camp == UnitCamp.Friend && (_playerUnitId == 0 || _playerUnitId == unit.Id))
            {
                _playerUnitId = unit.Id;
                RefreshPlayer(unit);
                RefreshEmitterOwnedBarWidget();
                RefreshEmitterAddPanelWidget();
            }

            if (unit.IsBoss && (_bossUnitId == 0 || _bossUnitId == unit.Id))
            {
                _bossUnitId = unit.Id;
                RefreshBoss(unit);
            }
        }

        public void OnUnitDead(BattleUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            if (unit.Id == _playerUnitId)
            {
                SetPlayerHp(0, GetMaxHp(unit));
            }

            if (unit.Id == _bossUnitId)
            {
                SetBossVisible(false);
                _bossUnitId = 0;
            }

            if (unit.Camp == UnitCamp.Enemy && !unit.IsBoss)
            {
                ShowMonsterDeathReward();
            }
        }

        public void SetWave(int currentWave, int totalWaves)
        {
            if (_tmpWave == null)
            {
                return;
            }

            _tmpWave.SetText(totalWaves > 0 ? $"第 {currentWave}/{totalWaves} 波" : "第 0/0 波");
        }

        public void SetWaveComplete(int waveNumber, int totalWaves)
        {
            if (_tmpWave == null)
            {
                return;
            }

            _tmpWave.SetText(totalWaves > 0 ? $"第 {waveNumber}/{totalWaves} 波完成" : "波次完成");
        }

        public void SetControlMode(int mode)
        {
            _currentControlMode = mode;
            RefreshControlModeLabel();
        }

        private void ResetBattleData()
        {
            _battle = null;
            _playerUnitId = 0;
            _bossUnitId = 0;
            _selectedEmitterVehicleId = 0;
            CloseDebugSubPanels();
            SetPlayerHp(0, 0);
            SetBossVisible(false);
            SetWave(0, 0);
            RefreshControlModeLabel();
            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshBuffAddPanelWidget();
        }

        private void RefreshPlayer(BattleUnit unit)
        {
            GetHpValues(unit, out int hp, out int maxHp);
            SetPlayerHp(hp, maxHp);
        }

        private void RefreshBoss(BattleUnit unit)
        {
            GetHpValues(unit, out int hp, out int maxHp);
            SetBossVisible(true);
            if (m_imgboss_hp != null)
            {
                m_imgboss_hp.fillAmount = GetHpFill(hp, maxHp);
            }

            _tmpBossName?.SetText(GetBattleUnitName(unit, "Boss"));
            _tmpBossHp?.SetText(maxHp > 0 ? $"{hp}/{maxHp}" : "0/0");
        }

        private void SetPlayerHp(int hp, int maxHp)
        {
            hp = Mathf.Clamp(hp, 0, Mathf.Max(0, maxHp));
            if (m_imgplayer_hp != null)
            {
                m_imgplayer_hp.fillAmount = GetHpFill(hp, maxHp);
            }

            _tmpPlayerHp?.SetText(maxHp > 0 ? $"{hp}/{maxHp}" : "0/0");
        }

        private void SetBossVisible(bool visible)
        {
            SetActive(m_gobossHp, visible);

            if (!visible)
            {
                if (m_imgboss_hp != null) m_imgboss_hp.fillAmount = 0f;
                _tmpBossHp?.SetText("0/0");
            }
        }

        private void RefreshControlModeLabel()
        {
            if (_tmpControlMode == null)
            {
                return;
            }

            _tmpControlMode.SetText(_currentControlMode.HasValue ? $"模式 {_currentControlMode.Value}" : "自动战斗");
        }

        private void CacheLayoutRefs()
        {
            _tmpBossName = FindUIText("TopCenterStatus/top_long_bar/m_tmpBossName");
            _tmpBossHp = FindUIText("TopCenterStatus/top_long_bar/m_tmpBossHp");
            _tmpWave = FindUIText("wave_panel_bg/m_tmpWave");
            _tmpPlayerHp = FindUIText("playerstatus/m_tmpPlayerHp");
            _tmpControlMode = FindUIText("playerstatus/m_tmpControlMode");
            _tmpEmitterInfo = m_itemvehicle?.transform.Find("m_tmpInfo")?.GetComponent<UIText>();
            _tfEmitterBuffRoot = m_itemvehicle?.transform.Find("m_tfbuff") as RectTransform;
            _gobuffTemplate = _tfEmitterBuffRoot != null ? _tfEmitterBuffRoot.Find("m_gobuff")?.gameObject : null;
        }

        private UIText FindUIText(string path)
        {
            return transform.Find(path)?.GetComponent<UIText>();
        }

        private static string GetBattleUnitName(BattleUnit unit, string fallback)
        {
            if (unit == null)
            {
                return fallback;
            }

            UnitConfig unitConfig = ConfigHelper.UnitConfig?.GetOrDefault(unit.ConfigId);
            if (unitConfig != null && !string.IsNullOrEmpty(unitConfig.Name))
            {
                return unitConfig.Name;
            }

            MonsterUnitConfig monsterConfig = ConfigHelper.MonsterUnitConfig?.GetOrDefault(unit.ConfigId);
            if (monsterConfig != null && !string.IsNullOrEmpty(monsterConfig.Name))
            {
                return monsterConfig.Name;
            }

            return unit.ConfigId > 0 ? $"{fallback} {unit.ConfigId}" : fallback;
        }

        private static void GetHpValues(BattleUnit unit, out int hp, out int maxHp)
        {
            hp = 0;
            maxHp = 0;
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            BattleStatsComponent stats = unit.GetComponent<BattleStatsComponent>();
            if (stats != null)
            {
                hp = stats.Hp;
                maxHp = stats.MaxHp;
                return;
            }

            NumericComponent numeric = unit.GetComponent<NumericComponent>();
            if (numeric == null)
            {
                return;
            }

            hp = numeric.GetAsInt(NumericType.Hp);
            maxHp = numeric.GetAsInt(NumericType.MaxHp);
        }

        private static int GetMaxHp(BattleUnit unit)
        {
            GetHpValues(unit, out _, out int maxHp);
            return maxHp;
        }

        private static float GetHpFill(int hp, int maxHp)
        {
            if (maxHp <= 0)
            {
                return 0f;
            }

            return Mathf.Clamp01((float)hp / maxHp);
        }

        private static void SetActive(GameObject gameObject, bool active)
        {
            if (gameObject != null && gameObject.activeSelf != active)
            {
                gameObject.SetActive(active);
            }
        }

        private void CreateEmitterAddPanelWidget()
        {
            RectTransform parent = gameObject.transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            _emitterAddPanelWidget = CreateWidgetByType<BattleEmitterAddPanelWidget>(parent);
            if (_emitterAddPanelWidget == null)
            {
                return;
            }

            _emitterAddPanelWidget.SetAddEmitterHandler(AddEmitterToPlayer);
            _emitterAddPanelWidget.SetPanelVisible(false);
        }

        private void RefreshEmitterOwnedBarWidget()
        {
            if (m_itemvehicle == null)
            {
                return;
            }

            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId) ?? GetLastEquippedVehicle(vehicleComponent);
            bool hasVehicle = player != null && vehicle != null && vehicle.State == VehicleState.Equipped;
            SetActive(m_itemvehicle, hasVehicle);
            if (!hasVehicle)
            {
                SetEmitterBuffIconCount(0);
                return;
            }

            _selectedEmitterVehicleId = vehicle.VehicleId;
            BattleAttackRuntime attack = FindAttackRuntime(player.GetComponent<BattleAttackComponent>(), vehicle.VehicleId);
            string name = GetEmitterDisplayName(vehicle);
            string cooldown = attack != null ? $"{attack.CooldownMs}ms" : $"{vehicle.AttackCooldownMs}ms";
            float range = attack?.AttackRange ?? vehicle.AttackRange;
            float hit = attack?.AttackHitRatio ?? vehicle.AttackHitRatio;
            float damage = attack?.WhiteDamageMultiplier ?? vehicle.WhiteDamageMultiplier;
            int slotCount = System.Math.Max(0, vehicle.BuffSlotCount);
            int usedCount = vehicle.SlottedEffectPackIds?.Count ?? 0;
            _tmpEmitterInfo?.SetText($"{name}\nCD {cooldown}  射程 {range:0.0}\n命中 {hit:0.00}  倍率 x{damage:0.0}\nBuff {usedCount}/{slotCount}");
            SetEmitterBuffIconCount(usedCount);
        }

        private void CreateBuffAddPanelWidget()
        {
            RectTransform parent = gameObject.transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            _buffAddPanelWidget = CreateWidgetByType<BattleBuffAddPanelWidget>(parent);
            if (_buffAddPanelWidget == null)
            {
                return;
            }

            _buffAddPanelWidget.SetAddBuffHandler(AddBuffToSelectedEmitter);
            _buffAddPanelWidget.SetPanelVisible(false);
        }

        private void RefreshBuffAddPanelWidget()
        {
            _buffAddPanelWidget?.SetSelectedEmitter(_selectedEmitterVehicleId);
            _buffAddPanelWidget?.Refresh();
        }

        private void CreateGMWidget()
        {
            RectTransform parent = gameObject.transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            _gmWidget = CreateWidgetByType<BattleGMWidget>(parent, false);
            if (_gmWidget == null)
            {
                return;
            }

            _gmWidget.SetHandlers(
                ToggleEmitterPanel,
                DeleteLastEmitter,
                ToggleBuffPanel,
                SpawnDebugEnemy);
        }

        private void ToggleGMWidget()
        {
            if (_gmWidget == null)
            {
                return;
            }

            bool visible = !_gmWidget.Visible;
            _gmWidget.Visible = visible;
            if (!visible)
            {
                CloseDebugSubPanels();
            }

            RefreshDebugButtonStates();
        }

        private void ToggleBuffPanel()
        {
            if (_buffAddPanelWidget == null)
            {
                return;
            }

            bool visible = !_buffAddPanelWidget.Visible;
            if (visible)
            {
                _emitterAddPanelWidget?.SetPanelVisible(false);
            }

            _buffAddPanelWidget.SetPanelVisible(visible);
            if (_buffAddPanelWidget.Visible)
            {
                RefreshBuffAddPanelWidget();
            }

            RefreshDebugButtonStates();
        }

        private void RefreshEmitterAddPanelWidget()
        {
            _emitterAddPanelWidget?.Refresh(_battle, _playerUnitId);
        }

        private static BattleAttackRuntime FindAttackRuntime(BattleAttackComponent attackComponent, long vehicleId)
        {
            if (attackComponent == null || vehicleId == 0)
            {
                return null;
            }

            foreach (BattleAttackRuntime attack in attackComponent.Attacks)
            {
                if (attack != null && attack.AttackRuntimeId == vehicleId)
                {
                    return attack;
                }
            }

            return null;
        }

        private static string GetEmitterDisplayName(VehicleData vehicle)
        {
            if (vehicle == null)
            {
                return "发射器";
            }

            EmitterConfig config = ConfigHelper.EmitterConfig?.GetOrDefault(vehicle.VehicleConfigId);
            if (config != null && !string.IsNullOrEmpty(config.Name))
            {
                return config.Name;
            }

            return vehicle.VehicleConfigId > 0 ? $"发射器 {vehicle.VehicleConfigId}" : "调试发射器";
        }

        private void SetEmitterBuffIconCount(int count)
        {
            SetActive(_gobuffTemplate, count > 0);
        }

        private void ToggleEmitterPanel()
        {
            if (_emitterAddPanelWidget == null)
            {
                return;
            }

            bool visible = !_emitterAddPanelWidget.Visible;
            if (visible)
            {
                _buffAddPanelWidget?.SetPanelVisible(false);
            }

            _emitterAddPanelWidget.SetPanelVisible(visible);
            if (_emitterAddPanelWidget.Visible)
            {
                RefreshEmitterAddPanelWidget();
            }

            RefreshDebugButtonStates();
        }

        private void AddEmitterToPlayer(EmitterConfig config)
        {
            if (config == null)
            {
                return;
            }

            BattleUnit player = ResolvePlayerUnit();
            if (player == null)
            {
                return;
            }

            SkillTargetingConfig targetingConfig = ConfigHelper.SkillTargetingConfig?.GetOrDefault(config.TargetingConfigId);
            float attackRange = targetingConfig != null ? targetingConfig.CastRange + targetingConfig.EdgeDistance : 1.5f;
            float attackHitRatio = config.AttackHitRatio > 0f && config.AttackHitRatio <= 1f ? config.AttackHitRatio : 0.5f;

            VehicleComponent vehicleComponent = player.GetComponent<VehicleComponent>() ?? player.AddComponent<VehicleComponent>();
            int beforeCount = GetEquippedVehicleCount(vehicleComponent);
            int maxLevel = GetEmitterMaxLevel(config);
            VehicleData vehicle = vehicleComponent.AddOrUpgradeVehicle(config.Id, maxLevel, out bool upgraded);
            bool wasEquipped = vehicle.State == VehicleState.Equipped;
            if (!upgraded && beforeCount >= MaxDebugEmitterSlots)
            {
                vehicleComponent.OwnedVehicles.Remove(vehicle);
                return;
            }

            ApplyEmitterLevelStats(vehicle, config, ResolveEmitterUpgradeLevelConfig(config, vehicle.Level), maxLevel, attackRange);
            vehicle.AttackHitRatio = attackHitRatio;
            vehicle.CanMoveCast = config.CanMoveCast;
            vehicle.BuffSlotCount = System.Math.Max(0, config.BuffSlotCount);
            if (!upgraded)
            {
                vehicle.SlottedEffectPackIds ??= new List<int>();
                vehicle.SlottedEffectPackIds.Clear();
                vehicle.SlottedBuffIds ??= new List<int>();
                vehicle.SlottedBuffIds.Clear();
            }

            if (upgraded && !wasEquipped && beforeCount >= MaxDebugEmitterSlots)
            {
                RefreshEmitterOwnedBarWidget();
                RefreshEmitterAddPanelWidget();
                RefreshBuffAddPanelWidget();
                return;
            }

            vehicle.State = VehicleState.Equipped;
            vehicleComponent.EquippedVehicleId = vehicle.VehicleId;
            vehicleComponent.EquippedVehicle = vehicle;
            _selectedEmitterVehicleId = vehicle.VehicleId;
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshBuffAddPanelWidget();
            _emitterAddPanelWidget.SetPanelVisible(false);
        }

        private void CreateEmitter()
        {
            BattleUnit player = ResolvePlayerUnit();
            if (player == null)
            {
                return;
            }

            VehicleComponent vehicleComponent = player.GetComponent<VehicleComponent>() ?? player.AddComponent<VehicleComponent>();
            if (GetEquippedVehicleCount(vehicleComponent) >= MaxDebugEmitterSlots)
            {
                return;
            }

            VehicleData vehicle = vehicleComponent.AddNewVehicle(0);
            vehicle.AttackCooldownMs = 1000;
            vehicle.AttackRange = 3f;
            vehicle.AttackHitRatio = 0.1f;
            vehicle.BaseDamage = 5f;
            vehicle.WhiteAttackRatio = 1.0f;
            vehicle.WhiteDamageMultiplier = 1.0f;
            vehicle.BuffSlotCount = 3;
            vehicle.CanMoveCast = false;
            vehicle.State = VehicleState.Equipped;
            vehicle.SlottedEffectPackIds ??= new List<int>();
            vehicle.SlottedEffectPackIds.Clear();
            vehicle.SlottedBuffIds ??= new List<int>();
            vehicle.SlottedBuffIds.Clear();

            vehicleComponent.EquippedVehicleId = vehicle.VehicleId;
            vehicleComponent.EquippedVehicle = vehicle;
            _selectedEmitterVehicleId = vehicle.VehicleId;
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshBuffAddPanelWidget();
        }

        private static void ApplyEmitterLevelStats(VehicleData vehicle, EmitterConfig config, EmitterUpgradeConfig levelConfig, int maxLevel, float baseAttackRange)
        {
            if (vehicle == null || config == null)
            {
                return;
            }

            int levelCap = maxLevel > 0 ? maxLevel : 1;
            vehicle.Level = System.Math.Min(levelCap, System.Math.Max(1, vehicle.Level));
            int baseCooldownMs = config.CooldownMs > 0 ? config.CooldownMs : 1000;
            int cooldownMs = baseCooldownMs - (levelConfig?.CooldownReduceMs ?? 0);
            vehicle.AttackCooldownMs = System.Math.Max(100, cooldownMs);
            vehicle.AttackRange = System.Math.Max(0.1f, (baseAttackRange > 0f ? baseAttackRange : 1.5f) + (levelConfig?.RangeAdd ?? 0f));
            vehicle.BaseDamage = config.BaseDamage;
            vehicle.WhiteAttackRatio = config.WhiteAttackRatio;
            vehicle.WhiteDamageMultiplier = System.Math.Max(0.1f, levelConfig?.WhiteDamageMultiplier ?? 1.0f);
        }

        private static int GetEmitterMaxLevel(EmitterConfig config)
        {
            if (config == null)
            {
                return 1;
            }

            int maxLevel = 1;
            if (ConfigHelper.EmitterUpgradeConfig?.DataList == null)
            {
                if (config.UpgradeConfigId > 0)
                {
                    Log.Error($"Emitter upgrade table missing: emitterId={config.Id}, upgradeConfigId={config.UpgradeConfigId}");
                }

                return maxLevel;
            }

            bool found = false;
            foreach (EmitterUpgradeConfig levelConfig in ConfigHelper.EmitterUpgradeConfig.DataList)
            {
                if (levelConfig != null && levelConfig.UpgradeConfigId == config.UpgradeConfigId)
                {
                    found = true;
                    maxLevel = System.Math.Max(maxLevel, levelConfig.Level);
                }
            }

            if (!found && config.UpgradeConfigId > 0)
            {
                Log.Error($"Emitter upgrade config missing: emitterId={config.Id}, upgradeConfigId={config.UpgradeConfigId}");
            }

            return maxLevel;
        }

        private static EmitterUpgradeConfig ResolveEmitterUpgradeLevelConfig(EmitterConfig config, int level)
        {
            if (config == null || ConfigHelper.EmitterUpgradeConfig?.DataList == null)
            {
                return null;
            }

            int targetLevel = System.Math.Max(1, level);
            foreach (EmitterUpgradeConfig levelConfig in ConfigHelper.EmitterUpgradeConfig.DataList)
            {
                if (levelConfig != null && levelConfig.UpgradeConfigId == config.UpgradeConfigId && levelConfig.Level == targetLevel)
                {
                    return levelConfig;
                }
            }

            if (config.UpgradeConfigId > 0)
            {
                Log.Error($"Emitter upgrade level missing: emitterId={config.Id}, upgradeConfigId={config.UpgradeConfigId}, level={targetLevel}");
            }

            return null;
        }

        private void DeleteLastEmitter()
        {
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            if (player == null || vehicleComponent == null)
            {
                return;
            }

            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (vehicle == null)
            {
                return;
            }

            vehicle.State = VehicleState.Stored;
            if (vehicleComponent.EquippedVehicleId == vehicle.VehicleId)
            {
                vehicleComponent.EquippedVehicleId = 0;
                vehicleComponent.EquippedVehicle = null;
            }
            if (_selectedEmitterVehicleId == vehicle.VehicleId)
            {
                VehicleData nextVehicle = GetLastEquippedVehicle(vehicleComponent);
                _selectedEmitterVehicleId = nextVehicle != null && nextVehicle.State == VehicleState.Equipped ? nextVehicle.VehicleId : 0;
            }

            BattleAttackComponent attackComponent = player.GetComponent<BattleAttackComponent>();
            attackComponent?.ResetEmitterCooldown(vehicle.VehicleId);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshBuffAddPanelWidget();
            if (_selectedEmitterVehicleId == 0)
            {
                _buffAddPanelWidget?.SetPanelVisible(false);
            }
        }

        private void AddBuffToSelectedEmitter(EmitterEffectPackConfig config)
        {
            if (config == null)
            {
                return;
            }

            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            if (player == null || vehicleComponent == null)
            {
                return;
            }

            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (vehicle == null)
            {
                return;
            }

            vehicle.SlottedEffectPackIds ??= new List<int>();
            if (vehicle.BuffSlotCount <= 0)
            {
                Log.Error($"Emitter has no effect slots: vehicleId={vehicle.VehicleId}, configId={vehicle.VehicleConfigId}, effectPackId={config.Id}");
                return;
            }

            if (vehicle.SlottedEffectPackIds.Count >= vehicle.BuffSlotCount)
            {
                Log.Error($"Emitter effect slots full: vehicleId={vehicle.VehicleId}, configId={vehicle.VehicleConfigId}, slotCount={vehicle.BuffSlotCount}, effectPackId={config.Id}");
                return;
            }

            vehicle.SlottedEffectPackIds.Add(config.Id);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshBuffAddPanelWidget();
            if (vehicle.SlottedEffectPackIds.Count >= vehicle.BuffSlotCount)
            {
                _buffAddPanelWidget?.SetPanelVisible(false);
            }
        }

        private void ShowMonsterDeathReward()
        {
            if (_buffAddPanelWidget == null)
            {
                return;
            }

            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData rewardTarget = GetRewardTargetVehicle(vehicleComponent);
            if (rewardTarget == null)
            {
                return;
            }

            List<EmitterEffectPackConfig> options = BuildMonsterDeathRewardOptions(rewardTarget);
            if (options.Count == 0)
            {
                return;
            }

            _selectedEmitterVehicleId = rewardTarget.VehicleId;
            _emitterAddPanelWidget?.SetPanelVisible(false);
            _buffAddPanelWidget.ShowRewardOptions(options, rewardTarget.VehicleId);
            RefreshBuffAddPanelWidget();
            RefreshDebugButtonStates();
        }

        private static VehicleData GetRewardTargetVehicle(VehicleComponent vehicleComponent)
        {
            if (vehicleComponent == null)
            {
                return null;
            }

            VehicleData fallback = null;
            foreach (VehicleData vehicle in vehicleComponent.OwnedVehicles)
            {
                if (vehicle == null || vehicle.State != VehicleState.Equipped)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = vehicle;
                }

                int usedCount = vehicle.SlottedEffectPackIds?.Count ?? 0;
                if (vehicle.BuffSlotCount > 0 && usedCount < vehicle.BuffSlotCount)
                {
                    return vehicle;
                }
            }

            if (fallback == null && vehicleComponent.EquippedVehicle != null)
            {
                fallback = vehicleComponent.EquippedVehicle;
            }

            if (fallback == null)
            {
                return null;
            }

            int fallbackUsedCount = fallback.SlottedEffectPackIds?.Count ?? 0;
            return fallback.BuffSlotCount > 0 && fallbackUsedCount < fallback.BuffSlotCount ? fallback : null;
        }

        private static List<EmitterEffectPackConfig> BuildMonsterDeathRewardOptions(VehicleData targetVehicle)
        {
            List<EmitterEffectPackConfig> options = new List<EmitterEffectPackConfig>(MonsterDeathRewardOptionCount);
            List<EmitterEffectPackConfig> dataList = ConfigHelper.EmitterEffectPackConfig?.DataList;
            if (targetVehicle == null || dataList == null || dataList.Count == 0)
            {
                return options;
            }

            HashSet<int> slottedIds = targetVehicle.SlottedEffectPackIds != null
                ? new HashSet<int>(targetVehicle.SlottedEffectPackIds)
                : new HashSet<int>();
            List<EmitterEffectPackConfig> pool = new List<EmitterEffectPackConfig>(dataList.Count);
            foreach (EmitterEffectPackConfig config in dataList)
            {
                if (config != null
                    && config.EffectIds != null
                    && config.EffectIds.Length > 0
                    && !slottedIds.Contains(config.Id))
                {
                    pool.Add(config);
                }
            }

            while (pool.Count > 0 && options.Count < MonsterDeathRewardOptionCount)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                options.Add(pool[index]);
                pool.RemoveAt(index);
            }

            return options;
        }

        private void SpawnDebugEnemy()
        {
            if (_battle == null || _battle.IsDisposed || _battle.State != BattleState.Fighting)
            {
                return;
            }

            int buffGroupId = ResolveDefaultBuffGroupId();
            BattleDebugSpawnHelper.SpawnMonster(
                _battle,
                100,
                10,
                0,
                2.5f,
                1,
                2f,
                0.5f,
                new List<BattleDebugEmitterSpec>
                {
                    new()
                    {
                        CooldownMs = 1200,
                        Range = 2.5f,
                        AttackHitRatio = 0.1f,
                        CanMoveCast = false,
                        PayloadType = BattleAttackPayloadType.VehicleBuff,
                        BuffGroupIds = buffGroupId > 0 ? new List<int> { buffGroupId } : new List<int>(),
                    },
                });
        }

        private static void SyncPlayerAttackFromVehicles(BattleUnit player)
        {
            if (player == null || player.IsDisposed)
            {
                return;
            }

            BattleAttackComponent attackComponent = player.GetComponent<BattleAttackComponent>() ?? player.AddComponent<BattleAttackComponent>();
            attackComponent.SyncFromVehicleComponent(player.GetComponent<VehicleComponent>());
        }

        private static int GetEquippedVehicleCount(VehicleComponent vehicleComponent)
        {
            if (vehicleComponent == null)
            {
                return 0;
            }

            int count = 0;
            foreach (VehicleData vehicle in vehicleComponent.OwnedVehicles)
            {
                if (vehicle != null && vehicle.State == VehicleState.Equipped)
                {
                    count++;
                }
            }

            if (count == 0 && vehicleComponent.EquippedVehicle != null)
            {
                count = 1;
            }

            return count;
        }

        private static VehicleData GetLastEquippedVehicle(VehicleComponent vehicleComponent)
        {
            if (vehicleComponent == null)
            {
                return null;
            }

            for (int i = vehicleComponent.OwnedVehicles.Count - 1; i >= 0; i--)
            {
                VehicleData vehicle = vehicleComponent.OwnedVehicles[i];
                if (vehicle != null && vehicle.State == VehicleState.Equipped)
                {
                    return vehicle;
                }
            }

            return vehicleComponent.EquippedVehicle;
        }

        private static VehicleData GetEquippedVehicle(VehicleComponent vehicleComponent, long vehicleId)
        {
            if (vehicleComponent == null || vehicleId == 0)
            {
                return null;
            }

            foreach (VehicleData vehicle in vehicleComponent.OwnedVehicles)
            {
                if (vehicle != null && vehicle.VehicleId == vehicleId && vehicle.State == VehicleState.Equipped)
                {
                    return vehicle;
                }
            }

            return vehicleComponent.EquippedVehicle != null && vehicleComponent.EquippedVehicle.VehicleId == vehicleId
                ? vehicleComponent.EquippedVehicle
                : null;
        }

        private BattleUnit ResolvePlayerUnit()
        {
            if (_battle == null || _battle.IsDisposed)
            {
                return null;
            }

            if (_playerUnitId != 0)
            {
                BattleUnit unit = _battle.GetChild<BattleUnit>(_playerUnitId);
                if (unit != null && !unit.IsDisposed)
                {
                    return unit;
                }
            }

            foreach (BattleUnit unit in _battle.GetAllBattleUnits())
            {
                if (unit != null && !unit.IsDisposed && unit.Camp == UnitCamp.Friend)
                {
                    _playerUnitId = unit.Id;
                    return unit;
                }
            }

            return null;
        }

        private void RefreshDebugButtonStates()
        {
            bool hasBattle = _battle != null && !_battle.IsDisposed && _battle.State == BattleState.Fighting;
            BattleUnit player = hasBattle ? ResolvePlayerUnit() : null;
            BattleAttackComponent attackComponent = player?.GetComponent<BattleAttackComponent>();
            int attackCount = attackComponent?.Attacks.Count ?? 0;
            int emitterCount = GetEquippedVehicleCount(player?.GetComponent<VehicleComponent>());
            if (emitterCount == 0 && attackCount > 0)
            {
                emitterCount = attackCount;
            }

            VehicleData selectedVehicle = GetEquippedVehicle(player?.GetComponent<VehicleComponent>(), _selectedEmitterVehicleId);
            bool canAddEffect = selectedVehicle != null
                                && selectedVehicle.BuffSlotCount > 0
                                && (selectedVehicle.SlottedEffectPackIds?.Count ?? 0) < selectedVehicle.BuffSlotCount;

            _gmWidget?.Refresh(
                player != null && emitterCount < MaxDebugEmitterSlots,
                player != null && emitterCount > 0,
                player != null && canAddEffect,
                hasBattle);
        }

        private void CloseDebugSubPanels()
        {
            _emitterAddPanelWidget?.SetPanelVisible(false);
            _buffAddPanelWidget?.SetPanelVisible(false);
        }

        private void EnsureSelectedEmitter()
        {
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            if (GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId) != null)
            {
                return;
            }

            VehicleData fallback = GetLastEquippedVehicle(vehicleComponent);
            _selectedEmitterVehicleId = fallback != null && fallback.State == VehicleState.Equipped ? fallback.VehicleId : 0;
        }

        private static int ResolveDefaultBuffGroupId()
        {
            if (ConfigHelper.BuffGroupConfig?.GetOrDefault(DefaultDebugBuffGroupId) != null)
            {
                return DefaultDebugBuffGroupId;
            }

            return ConfigHelper.BuffGroupConfig?.DataList != null && ConfigHelper.BuffGroupConfig.DataList.Count > 0
                ? ConfigHelper.BuffGroupConfig.DataList[0].Id
                : 0;
        }
    }
}
