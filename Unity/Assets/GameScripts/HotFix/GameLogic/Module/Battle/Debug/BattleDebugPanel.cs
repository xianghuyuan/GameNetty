using Cysharp.Threading.Tasks;
using UnityEngine;
using ET;

namespace GameLogic
{
    public class BattleDebugPanel : MonoBehaviour
    {
        private static BattleDebugPanel _instance;

        private bool _showPanel;
        private Rect _windowRect = new Rect(10, 10, 500, 750);

        // 怪物配置
        private int _spawnCount = 1;
        private int _monsterHp = 1;
        private int _monsterAtk = 10;
        private int _monsterDef = 1;
        private float _monsterSpeed = 1f;
        private float _spawnOffset = 1f;
        private float _spreadRange = 3f;

        // 玩家操作
        private int _healAmount = 500;
        private long _cachedPlayerUnitId;
        private int _cachedPlayerConfigId = 1001;

        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _sectionStyle;
        private GUIStyle _textFieldStyle;
        private bool _stylesInitialized;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void EnsureStyles()
        {
            if (_stylesInitialized) return;
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 18,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleLeft,
            };
            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
            };
            _sectionStyle = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
            };
            _textFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
            };
            _stylesInitialized = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F2))
            {
                _showPanel = !_showPanel;
            }
        }

        private void OnGUI()
        {
            if (!_showPanel) return;
            EnsureStyles();

            float scale = Mathf.Max(Screen.width / 1920f, Screen.height / 1080f);
            scale = Mathf.Max(scale, 1f);

            Matrix4x4 oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one * scale);

            _windowRect = GUI.Window(9999, _windowRect, DrawWindow, "Battle Debug [F2]");

            GUI.matrix = oldMatrix;
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            Battle battle = GetCurrentBattle();

            // === 战斗状态 ===
            if (battle == null)
            {
                GUILayout.Label("<color=red>No active battle</color>", _labelStyle);
                if (GUILayout.Button("Start Offline Battle", _buttonStyle, GUILayout.Height(40)))
                {
                    StartOfflineBattle();
                }
            }
            else
            {
                string c = battle.State == BattleState.Fighting ? "green" : "yellow";
                GUILayout.Label($"Battle: <color={c}>{battle.State}</color>", _labelStyle);
            }

            GUILayout.Space(4);
            GUILayout.Box("", GUILayout.Height(2));
            GUILayout.Space(4);

            // === 玩家操作 ===
            GUILayout.Label("<color=cyan>-- Player --</color>", _sectionStyle);

            BattleUnit player = GetPlayerUnit(battle);
            if (player != null)
            {
                NumericComponent num = player.GetComponent<NumericComponent>();
                int hp = num?.GetAsInt(NumericType.Hp) ?? 0;
                int maxHp = num?.GetAsInt(NumericType.MaxHp) ?? 0;
                int atk = num?.GetAsInt(NumericType.Attack) ?? 0;
                bool dead = player.IsDead;

                string hpColor = dead ? "red" : (hp < maxHp * 0.3f ? "yellow" : "green");
                GUILayout.Label($"HP: <color={hpColor}>{hp}/{maxHp}</color>  ATK: {atk}  {(dead ? "<color=red>DEAD</color>" : "")}", _labelStyle);

                GUILayout.BeginHorizontal();
                GUILayout.Label("Heal:", _labelStyle, GUILayout.Width(70));
                _healAmount = IntField(_healAmount, GUILayout.Width(80));
                if (GUILayout.Button($"+{_healAmount}", _buttonStyle, GUILayout.Width(80), GUILayout.Height(32)))
                {
                    HealPlayer(player, _healAmount);
                }
                if (GUILayout.Button("Full HP", _buttonStyle, GUILayout.Width(90), GUILayout.Height(32)))
                {
                    FullHealPlayer(player);
                }
                GUILayout.EndHorizontal();

                if (dead)
                {
                    if (GUILayout.Button("Revive Player", _buttonStyle, GUILayout.Height(38)))
                    {
                        RevivePlayer(player);
                    }
                }
            }
            else if (battle != null)
            {
                GUILayout.Label("<color=red>Player unit destroyed</color>", _labelStyle);
                if (GUILayout.Button("Rebuild Player", _buttonStyle, GUILayout.Height(38)))
                {
                    RebuildPlayer(battle);
                }
            }

            GUILayout.Space(4);
            GUILayout.Box("", GUILayout.Height(2));
            GUILayout.Space(4);

            // === 怪物生成 ===
            GUILayout.Label("<color=orange>-- Spawn Monster --</color>", _sectionStyle);

            GUILayout.BeginHorizontal();
            GUILayout.Label("HP:", _labelStyle, GUILayout.Width(70));
            _monsterHp = IntField(_monsterHp);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("ATK:", _labelStyle, GUILayout.Width(70));
            _monsterAtk = IntField(_monsterAtk);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("DEF:", _labelStyle, GUILayout.Width(70));
            _monsterDef = IntField(_monsterDef);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed:", _labelStyle, GUILayout.Width(70));
            _monsterSpeed = FloatField(_monsterSpeed);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Count:", _labelStyle, GUILayout.Width(70));
            _spawnCount = IntField(_spawnCount);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Offset:", _labelStyle, GUILayout.Width(70));
            _spawnOffset = Mathf.Round(GUILayout.HorizontalSlider(_spawnOffset, 0f, 15f) * 10f) / 10f;
            GUILayout.Label(_spawnOffset.ToString("F1"), _labelStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Spread:", _labelStyle, GUILayout.Width(70));
            _spreadRange = Mathf.Round(GUILayout.HorizontalSlider(_spreadRange, 0f, 10f) * 10f) / 10f;
            GUILayout.Label(_spreadRange.ToString("F1"), _labelStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            GUILayout.Space(6);

            GUI.enabled = battle != null && battle.State == BattleState.Fighting;
            if (GUILayout.Button("Spawn Monsters", _buttonStyle, GUILayout.Height(45)))
            {
                SpawnMonsters(battle);
            }
            GUI.enabled = true;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private int IntField(int val, params GUILayoutOption[] opts)
        {
            string s = GUILayout.TextField(val.ToString(), _textFieldStyle, opts);
            if (int.TryParse(s, out int v)) return v;
            return val;
        }

        private float FloatField(float val, params GUILayoutOption[] opts)
        {
            string s = GUILayout.TextField(val.ToString(), _textFieldStyle, opts);
            if (float.TryParse(s, out float v)) return v;
            return val;
        }

        private Battle GetCurrentBattle()
        {
            Scene root = Init.Root;
            if (root == null) return null;
            BattleComponent bc = root.GetComponent<BattleComponent>();
            return bc?.GetCurrentBattle();
        }

        private BattleUnit GetPlayerUnit(Battle battle)
        {
            if (battle == null) return null;

            // 先尝试从缓存 ID 查找
            if (_cachedPlayerUnitId > 0)
            {
                BattleUnit cached = battle.GetChild<BattleUnit>(_cachedPlayerUnitId);
                if (cached != null && !cached.IsDisposed) return cached;
            }

            // 遍历查找
            foreach (var child in battle.Children.Values)
            {
                if (child is BattleUnit unit && unit.Camp == UnitCamp.Friend)
                {
                    _cachedPlayerUnitId = unit.Id;
                    _cachedPlayerConfigId = unit.ConfigId;
                    return unit;
                }
            }
            return null;
        }

        private void HealPlayer(BattleUnit player, int amount)
        {
            if (player.IsDead) return;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num == null) return;
            int maxHp = num.GetAsInt(NumericType.MaxHp);
            int hp = num.GetAsInt(NumericType.Hp) + amount;
            if (hp > maxHp) hp = maxHp;
            player.SetNumeric(NumericType.Hp, hp);
        }

        private void FullHealPlayer(BattleUnit player)
        {
            if (player.IsDead) return;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num == null) return;
            player.SetNumeric(NumericType.Hp, num.GetAsInt(NumericType.MaxHp));
        }

        private void RevivePlayer(BattleUnit player)
        {
            if (!player.IsDead) return;
            player.IsDead = false;
            NumericComponent num = player.GetComponent<NumericComponent>();
            if (num != null)
            {
                player.SetNumeric(NumericType.Hp, num.GetAsInt(NumericType.MaxHp));
            }

            // 恢复视图
            BattleUnitView view = player.GetComponent<BattleUnitView>();
            if (view != null && !view.IsDisposed)
            {
                view.DeathCancelled = true;
                if (view.GameObject != null) view.GameObject.SetActive(true);
                if (view.SkeletonAnimation != null)
                {
                    view.CurrentAnimName = "idle";
                    view.SkeletonAnimation.AnimationState.SetAnimation(0, "idle", true);
                }
                view.Initialized = true;
            }

            // 重建 HUD
            BattleUIHelper.CreateUnitUI(player);

            EventSystem.Instance.Publish(player.Scene(), new BattleUnitRevived { BattleUnit = player });
        }

        /// <summary>
        /// 玩家死亡被 Dispose 后，重建一个新的玩家 BattleUnit
        /// </summary>
        private void RebuildPlayer(Battle battle)
        {
            long unitId = IdGenerater.Instance.GenerateInstanceId();

            BattleUnit unit = battle.AddChildWithId<BattleUnit, int>(unitId, _cachedPlayerConfigId);
            unit.Camp = UnitCamp.Friend;
            unit.Position = new Unity.Mathematics.float3(-5f, 0, 0);
            unit.Forward = new Unity.Mathematics.float3(1f, 0, 0);
            unit.FaceDirection = 1f;

            NumericComponent numeric = unit.AddComponent<NumericComponent>();
            numeric.Set(NumericType.Hp, 1000);
            numeric.Set(NumericType.MaxHp, 1000);
            numeric.Set(NumericType.Attack, 50);
            numeric.Set(NumericType.Defense, 10);
            numeric.Set(NumericType.Speed, 3f);

            unit.AddComponent<BattleUnitCombatComponent, float>(3f);

            BattleUnitCombatComponent combatComp = unit.GetComponent<BattleUnitCombatComponent>();
            if (combatComp != null)
            {
                combatComp.AutoSkillIds = new[] { 11001 };
            }

            unit.AddComponent<ClientPlayerAIComponent>();

            BattleUnitView view = unit.AddComponent<BattleUnitView, UnitCamp, Unity.Mathematics.float3>(unit.Camp, unit.Position);
            view.InitViewAsync().Forget();

            BattleUIHelper.CreateUnitUI(unit);

            // 确保 Player AI Tick
            if (battle.GetComponent<ClientPlayerAITickComponent>() == null)
            {
                battle.AddComponent<ClientPlayerAITickComponent>();
            }

            _cachedPlayerUnitId = unit.Id;
            Log.Info($"BattleDebug: Rebuilt player unit, id={unit.Id}");
        }

        private void SpawnMonsters(Battle battle)
        {
            BattleDebugSpawnHelper.SpawnMonster(battle, _monsterHp, _monsterAtk, _monsterDef, _monsterSpeed, _spawnCount, _spawnOffset, _spreadRange);
        }

        private void StartOfflineBattle()
        {
            Scene root = Init.Root;
            if (root == null) return;
            OfflineBattleHelper.StartOfflineBattle(root).Coroutine();
        }
    }
}
