using System.Collections.Generic;
using ET;
using UnityEngine;

namespace GameLogic
{
    public partial class BattleMainWindow : UIWindow
    {
        private partial void OnClickGearBtn()
        {
            ToggleEmitterPanel();
        }

        private const int MaxDebugEmitterSlots = 8;

        private partial void OnClickCreateEnemyBtn()
        {
            SpawnDebugEnemy();
        }

        private const int DefaultDebugBuffGroupId = 61021;

        private VehicleWidget _itemVehicleWidget;
        private float _playerHpWidth;

        
        private Battle _battle;
        

        private long _playerUnitId;
        private long _bossUnitId;
        private BattleEmitterAddPanelWidget _emitterAddPanelWidget;
        private BattleEmitterAdjustPanelWidget _emitterAdjustPanelWidget;
        private long _selectedEmitterVehicleId;

        protected override void OnCreate()
        {
            CacheLayoutRefs();
            CreateEmitterAddPanelWidget();
            CreateEmitterAdjustPanelWidget();
            ResetBattleData();
            BattleUIHelper.BindMainWindow(this);
        }

        protected override void OnDestroy()
        {
            BattleUIHelper.BindMainWindow(null);
            _battle = null;
            _emitterAddPanelWidget = null;
            _emitterAdjustPanelWidget = null;
            _itemVehicleWidget = null;
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

            if (_emitterAdjustPanelWidget != null && _emitterAdjustPanelWidget.Visible)
            {
                RefreshEmitterAdjustPanelWidget();
            }
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
            RefreshEmitterAdjustPanelWidget();
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
                RefreshEmitterAdjustPanelWidget();
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
        }

        public void SetWave(int currentWave, int totalWaves)
        {
        }

        public void SetWaveComplete(int waveNumber, int totalWaves)
        {
        }

        public void SetControlMode(int mode)
        {
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
            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
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

        }

        private void SetPlayerHp(int hp, int maxHp)
        {
            hp = Mathf.Clamp(hp, 0, Mathf.Max(0, maxHp));
            if (m_imgplayer_hp != null)
            {
                RectTransform hpRect = m_imgplayer_hp.rectTransform;
                Vector2 size = hpRect.sizeDelta;
                size.x = _playerHpWidth * GetHpFill(hp, maxHp);
                hpRect.sizeDelta = size;
            }

        }

        private void SetBossVisible(bool visible)
        {
            SetActive(m_gobossHp, visible);

            if (!visible)
            {
                if (m_imgboss_hp != null) m_imgboss_hp.fillAmount = 0f;
            }
        }

        private void CacheLayoutRefs()
        {
            _playerHpWidth = m_imgplayer_hp.rectTransform.sizeDelta.x;
            _itemVehicleWidget = CreateWidget<VehicleWidget>(m_itemvehicle);
            _itemVehicleWidget.SetClickHandler(OnClickEmitterItem);
        }

        private void OnClickEmitterItem()
        {
            ToggleEmitterAdjustPanel();
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

        private void CreateEmitterAdjustPanelWidget()
        {
            RectTransform parent = gameObject.transform as RectTransform;
            if (parent == null)
            {
                return;
            }

            _emitterAdjustPanelWidget = CreateWidgetByType<BattleEmitterAdjustPanelWidget>(parent);
            if (_emitterAdjustPanelWidget == null)
            {
                return;
            }

            _emitterAdjustPanelWidget.SetLevelChangedHandler(SetSelectedEmitterLevel);
            _emitterAdjustPanelWidget.SetAddBuffHandler(AddBuffToSelectedEmitter);
            _emitterAdjustPanelWidget.SetRemoveBuffHandler(RemoveBuffFromSelectedEmitter);
            _emitterAdjustPanelWidget.SetDeleteEmitterHandler(DeleteSelectedEmitter);
            _emitterAdjustPanelWidget.SetPanelVisible(false);
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
            _itemVehicleWidget?.SetPanelVisible(hasVehicle);
            if (!hasVehicle)
            {
                _itemVehicleWidget?.Refresh(string.Empty, 0);
                _emitterAdjustPanelWidget?.SetPanelVisible(false);
                return;
            }

            _selectedEmitterVehicleId = vehicle.VehicleId;
            BattleAttackRuntime attack = FindAttackRuntime(player.GetComponent<BattleAttackComponent>(), vehicle.VehicleId);
            string name = GetEmitterDisplayName(vehicle);
            string cooldown = attack != null ? $"{attack.CooldownMs}ms" : $"{vehicle.AttackCooldownMs}ms";
            float range = attack?.AttackRange ?? vehicle.AttackRange;
            float baseDamage = attack?.BaseDamage ?? vehicle.BaseDamage;
            float attackRatio = attack?.WhiteAttackRatio ?? vehicle.WhiteAttackRatio;
            int slotCount = System.Math.Max(0, vehicle.BuffSlotCount);
            int usedCount = vehicle.SlottedEffectPackIds?.Count ?? 0;
            _itemVehicleWidget?.Refresh($"{name}\n伤害 {baseDamage:0.#}+攻击 x{attackRatio:0.##}\nCD {cooldown}  射程 {range:0.0}\nBuff {usedCount}/{slotCount}", usedCount);
        }

        private void RefreshEmitterAddPanelWidget()
        {
            _emitterAddPanelWidget?.Refresh(_battle, _playerUnitId);
        }

        private void RefreshEmitterAdjustPanelWidget()
        {
            if (_emitterAdjustPanelWidget == null || !_emitterAdjustPanelWidget.Visible)
            {
                return;
            }

            EnsureSelectedEmitter();
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (vehicle == null)
            {
                _emitterAdjustPanelWidget.SetPanelVisible(false);
                return;
            }

            BattleAttackRuntime attack = FindAttackRuntime(player.GetComponent<BattleAttackComponent>(), vehicle.VehicleId);
            _emitterAdjustPanelWidget.Refresh(vehicle, attack);
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

        private void ToggleEmitterPanel()
        {
            if (_emitterAddPanelWidget == null)
            {
                return;
            }

            bool visible = !_emitterAddPanelWidget.Visible;
            if (visible)
            {
                _emitterAdjustPanelWidget?.SetPanelVisible(false);
            }

            _emitterAddPanelWidget.SetPanelVisible(visible);
            if (_emitterAddPanelWidget.Visible)
            {
                RefreshEmitterAddPanelWidget();
            }
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

            VehicleComponent vehicleComponent = player.GetComponent<VehicleComponent>() ?? player.AddComponent<VehicleComponent>();
            int beforeCount = GetEquippedVehicleCount(vehicleComponent);
            int maxLevel = EmitterUpgradeRuntimeHelper.ResolveMaxLevel(config);
            VehicleData vehicle = vehicleComponent.AddOrUpgradeVehicle(config.Id, maxLevel, out bool upgraded);
            bool wasEquipped = vehicle.State == VehicleState.Equipped;
            if (!upgraded && beforeCount >= MaxDebugEmitterSlots)
            {
                vehicleComponent.OwnedVehicles.Remove(vehicle);
                return;
            }

            ApplyEmitterLevelStats(vehicle, config, EmitterUpgradeRuntimeHelper.ResolveLevelConfig(config, vehicle.Level), maxLevel);
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
                return;
            }

            vehicle.State = VehicleState.Equipped;
            vehicleComponent.EquippedVehicleId = vehicle.VehicleId;
            vehicleComponent.EquippedVehicle = vehicle;
            _selectedEmitterVehicleId = vehicle.VehicleId;
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
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
            vehicle.BaseDamage = 5f;
            vehicle.WhiteAttackRatio = 1.0f;
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
            RefreshEmitterAdjustPanelWidget();
        }

        private void ToggleEmitterAdjustPanel()
        {
            if (_emitterAdjustPanelWidget == null)
            {
                return;
            }

            EnsureSelectedEmitter();
            if (_selectedEmitterVehicleId == 0)
            {
                _emitterAdjustPanelWidget.SetPanelVisible(false);
                return;
            }

            bool visible = !_emitterAdjustPanelWidget.Visible;
            if (visible)
            {
                _emitterAddPanelWidget?.SetPanelVisible(false);
            }

            _emitterAdjustPanelWidget.SetPanelVisible(visible);
            if (_emitterAdjustPanelWidget.Visible)
            {
                RefreshEmitterAdjustPanelWidget();
            }
        }

        private void SetSelectedEmitterLevel(int level)
        {
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (player == null || vehicle == null)
            {
                return;
            }

            EmitterConfig config = ConfigHelper.EmitterConfig?.GetOrDefault(vehicle.VehicleConfigId);
            if (config == null)
            {
                return;
            }

            int maxLevel = EmitterUpgradeRuntimeHelper.ResolveMaxLevel(config);
            int nextLevel = System.Math.Min(System.Math.Max(1, level), System.Math.Max(1, maxLevel));
            vehicle.Level = nextLevel;
            ApplyEmitterLevelStats(vehicle, config, EmitterUpgradeRuntimeHelper.ResolveLevelConfig(config, nextLevel), maxLevel);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
        }

        private void AddBuffToSelectedEmitter(int effectPackId)
        {
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (player == null || vehicle == null)
            {
                return;
            }

            EmitterEffectPackConfig config = ConfigHelper.EmitterEffectPackConfig?.GetOrDefault(effectPackId);
            if (config == null)
            {
                Log.Error($"[BattleMainWindow] Invalid emitter effect pack id={effectPackId}.");
                return;
            }

            vehicle.SlottedEffectPackIds ??= new List<int>();
            int slotCount = System.Math.Max(0, vehicle.BuffSlotCount);
            if (slotCount == 0 || vehicle.SlottedEffectPackIds.Count >= slotCount)
            {
                return;
            }

            if (vehicle.SlottedEffectPackIds.Contains(effectPackId))
            {
                return;
            }

            vehicle.SlottedEffectPackIds.Add(effectPackId);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
        }

        private void RemoveBuffFromSelectedEmitter(int slotIndex)
        {
            BattleUnit player = ResolvePlayerUnit();
            VehicleComponent vehicleComponent = player?.GetComponent<VehicleComponent>();
            VehicleData vehicle = GetEquippedVehicle(vehicleComponent, _selectedEmitterVehicleId);
            if (player == null || vehicle == null || vehicle.SlottedEffectPackIds == null)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= vehicle.SlottedEffectPackIds.Count)
            {
                return;
            }

            vehicle.SlottedEffectPackIds.RemoveAt(slotIndex);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
        }

        private static void ApplyEmitterLevelStats(VehicleData vehicle, EmitterConfig config, EmitterUpgradeConfig levelConfig, int maxLevel)
        {
            if (vehicle == null || config == null)
            {
                return;
            }

            int levelCap = maxLevel > 0 ? maxLevel : 1;
            vehicle.Level = System.Math.Min(levelCap, System.Math.Max(1, vehicle.Level));
            vehicle.AttackCooldownMs = EmitterUpgradeRuntimeHelper.ResolveCooldownMs(levelConfig);
            vehicle.AttackRange = EmitterUpgradeRuntimeHelper.ResolveRange(config, levelConfig);
            vehicle.BaseDamage = EmitterUpgradeRuntimeHelper.ResolveBaseDamage(levelConfig);
            vehicle.WhiteAttackRatio = EmitterUpgradeRuntimeHelper.ResolveAttackRatio(levelConfig);
        }

        private void DeleteSelectedEmitter()
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

            long removedVehicleId = vehicle.VehicleId;
            vehicleComponent.OwnedVehicles.Remove(vehicle);
            if (vehicleComponent.EquippedVehicleId == vehicle.VehicleId)
            {
                vehicleComponent.EquippedVehicleId = 0;
            }

            if (vehicleComponent.EquippedVehicle == vehicle)
            {
                vehicleComponent.EquippedVehicle = null;
            }

            if (_selectedEmitterVehicleId == removedVehicleId)
            {
                VehicleData nextVehicle = GetLastEquippedVehicle(vehicleComponent);
                _selectedEmitterVehicleId = nextVehicle != null && nextVehicle.State == VehicleState.Equipped ? nextVehicle.VehicleId : 0;
                vehicleComponent.EquippedVehicleId = _selectedEmitterVehicleId;
                vehicleComponent.EquippedVehicle = nextVehicle;
            }

            BattleAttackComponent attackComponent = player.GetComponent<BattleAttackComponent>();
            attackComponent?.ResetEmitterCooldown(removedVehicleId);
            SyncPlayerAttackFromVehicles(player);

            RefreshEmitterOwnedBarWidget();
            RefreshEmitterAddPanelWidget();
            RefreshEmitterAdjustPanelWidget();
            if (_selectedEmitterVehicleId == 0)
            {
                _emitterAdjustPanelWidget?.SetPanelVisible(false);
            }
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

        private void CloseDebugSubPanels()
        {
            _emitterAddPanelWidget?.SetPanelVisible(false);
            _emitterAdjustPanelWidget?.SetPanelVisible(false);
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
