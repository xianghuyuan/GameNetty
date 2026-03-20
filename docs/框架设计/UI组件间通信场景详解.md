# UI 组件间通信场景详解

## 一、常见 UI 通信场景分类

### 1. 父子窗口通信

#### 场景 1.1：主界面打开子面板并传递数据

```csharp
// 主界面打开背包面板
public class MainUI : UIWindow
{
    private void OnBagButtonClick()
    {
        // 方式 1：直接传参（推荐）
        GameModule.UI.ShowUIAsync<BagUI>(playerData, selectedTab);
        
        // 方式 2：通过事件
        GameEvent.Send("OnOpenBag", playerData.PlayerId);
    }
}

// 背包面板接收数据
public class BagUI : UIWindow
{
    protected override void OnRefresh(params object[] userDatas)
    {
        if (userDatas.Length > 0)
        {
            PlayerData playerData = userDatas[0] as PlayerData;
            int selectedTab = (int)userDatas[1];
            
            // 初始化背包数据
            InitBagData(playerData, selectedTab);
        }
    }
}
```

#### 场景 1.2：子面板关闭时通知父窗口

```csharp
// 子面板：物品详情面板
public class ItemDetailUI : UIWindow
{
    private void OnUseItemButtonClick()
    {
        // 使用物品后通知父窗口刷新
        GameEvent.Send("OnItemUsed", itemId);
        
        // 关闭自己
        GameModule.UI.CloseUI<ItemDetailUI>();
    }
}

// 父面板：背包面板
public class BagUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<int>("OnItemUsed", OnItemUsed);
    }
    
    protected override void OnDestroy()
    {
        GameEvent.RemoveEventListener<int>("OnItemUsed", OnItemUsed);
    }
    
    private void OnItemUsed(int itemId)
    {
        // 刷新背包显示
        RefreshBagItem(itemId);
    }
}
```

### 2. 平级窗口通信

#### 场景 2.1：背包 ↔ 装备面板

```csharp
// 背包面板：选择物品
public class BagUI : UIWindow
{
    private void OnItemClick(ItemData item)
    {
        // 通知装备面板显示物品详情
        GameEvent.Send("OnBagItemSelected", item);
    }
}

// 装备面板：接收选中的物品
public class EquipmentUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<ItemData>("OnBagItemSelected", OnBagItemSelected);
    }
    
    private void OnBagItemSelected(ItemData item)
    {
        // 显示物品可装备的位置
        HighlightEquipSlot(item.EquipType);
        
        // 显示对比信息
        ShowCompareInfo(item);
    }
}
```

#### 场景 2.2：技能栏 ↔ 技能书面板

```csharp
// 技能书面板：拖拽技能
public class SkillBookUI : UIWindow
{
    private void OnSkillDragStart(SkillData skill)
    {
        GameEvent.Send("OnSkillDragStart", skill);
    }
    
    private void OnSkillDragEnd()
    {
        GameEvent.Send("OnSkillDragEnd");
    }
}

// 技能栏：接收拖拽
public class SkillBarUI : UIWindow
{
    private SkillData draggingSkill;
    
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<SkillData>("OnSkillDragStart", OnSkillDragStart);
        GameEvent.AddEventListener("OnSkillDragEnd", OnSkillDragEnd);
    }
    
    private void OnSkillDragStart(SkillData skill)
    {
        draggingSkill = skill;
        // 高亮可放置的技能槽
        HighlightAvailableSlots(skill);
    }
    
    private void OnSkillDragEnd()
    {
        draggingSkill = null;
        // 取消高亮
        ClearHighlight();
    }
}
```

### 3. 全局通知类通信

#### 场景 3.1：货币变化通知所有相关 UI

```csharp
// 任何地方货币变化
public class ShopUI : UIWindow
{
    private void OnBuyItem(int itemId, int price)
    {
        // 购买成功后通知
        GameEvent.Send("OnCurrencyChanged", CurrencyType.Gold, -price);
    }
}

// 多个 UI 监听货币变化
public class MainUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<CurrencyType, int>("OnCurrencyChanged", OnCurrencyChanged);
    }
    
    private void OnCurrencyChanged(CurrencyType type, int delta)
    {
        // 更新顶部货币显示
        UpdateCurrencyDisplay(type);
    }
}

public class BagUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<CurrencyType, int>("OnCurrencyChanged", OnCurrencyChanged);
    }
    
    private void OnCurrencyChanged(CurrencyType type, int delta)
    {
        // 更新背包中的货币显示
        UpdateBagCurrency(type);
    }
}
```

#### 场景 3.2：等级提升通知

```csharp
// 等级提升事件
GameEvent.Send("OnPlayerLevelUp", newLevel);

// 多个 UI 响应
public class MainUI : UIWindow
{
    private void OnPlayerLevelUp(int level)
    {
        // 播放升级特效
        PlayLevelUpEffect();
        // 更新等级显示
        txtLevel.text = level.ToString();
    }
}

public class SkillUI : UIWindow
{
    private void OnPlayerLevelUp(int level)
    {
        // 检查是否有新技能解锁
        CheckNewSkillsUnlocked(level);
    }
}

public class AttributeUI : UIWindow
{
    private void OnPlayerLevelUp(int level)
    {
        // 刷新属性面板
        RefreshAttributes();
    }
}
```

### 4. 列表/网格组件通信

#### 场景 4.1：列表项选中通知

```csharp
// 列表项
public class BagItemCell : MonoBehaviour
{
    private ItemData itemData;
    
    public void OnClick()
    {
        // 通知列表有物品被选中
        GameEvent.Send("OnBagItemCellClick", itemData);
    }
}

// 背包面板
public class BagUI : UIWindow
{
    private BagItemCell selectedCell;
    
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<ItemData>("OnBagItemCellClick", OnItemCellClick);
    }
    
    private void OnItemCellClick(ItemData item)
    {
        // 取消之前的选中
        if (selectedCell != null)
        {
            selectedCell.SetSelected(false);
        }
        
        // 显示物品详情
        ShowItemDetail(item);
        
        // 通知其他面板
        GameEvent.Send("OnBagItemSelected", item);
    }
}
```

#### 场景 4.2：滚动列表加载更多

```csharp
// 滚动列表
public class ScrollListUI : UIWindow
{
    private void OnScrollValueChanged(Vector2 pos)
    {
        // 滚动到底部时通知需要加载更多
        if (pos.y <= 0.1f)
        {
            GameEvent.Send("OnScrollListNeedLoadMore");
        }
    }
}

// 数据管理器
public class DataManager
{
    private void Initialize()
    {
        GameEvent.AddEventListener("OnScrollListNeedLoadMore", OnLoadMore);
    }
    
    private void OnLoadMore()
    {
        // 加载更多数据
        LoadMoreData();
        
        // 通知列表刷新
        GameEvent.Send("OnDataLoaded", newDataList);
    }
}
```

### 5. 弹窗/对话框通信

#### 场景 5.1：确认对话框回调

```csharp
// 打开确认对话框
public class BagUI : UIWindow
{
    private void OnDeleteItemClick(ItemData item)
    {
        // 显示确认对话框
        var dialog = GameModule.UI.ShowUIAsync<ConfirmDialogUI>(
            "确认删除",
            $"确定要删除 {item.Name} 吗？",
            () => OnConfirmDelete(item),  // 确认回调
            () => OnCancelDelete()         // 取消回调
        );
    }
    
    private void OnConfirmDelete(ItemData item)
    {
        // 删除物品
        DeleteItem(item);
        
        // 刷新列表
        RefreshBagList();
    }
}

// 确认对话框
public class ConfirmDialogUI : UIWindow
{
    private Action onConfirm;
    private Action onCancel;
    
    protected override void OnRefresh(params object[] userDatas)
    {
        string title = userDatas[0] as string;
        string content = userDatas[1] as string;
        onConfirm = userDatas[2] as Action;
        onCancel = userDatas[3] as Action;
        
        txtTitle.text = title;
        txtContent.text = content;
    }
    
    private void OnConfirmButtonClick()
    {
        onConfirm?.Invoke();
        GameModule.UI.CloseUI<ConfirmDialogUI>();
    }
    
    private void OnCancelButtonClick()
    {
        onCancel?.Invoke();
        GameModule.UI.CloseUI<ConfirmDialogUI>();
    }
}
```

#### 场景 5.2：输入框回调

```csharp
// 打开输入框
public class FriendUI : UIWindow
{
    private void OnAddFriendClick()
    {
        GameModule.UI.ShowUIAsync<InputDialogUI>(
            "添加好友",
            "请输入好友ID",
            (string friendId) => OnInputComplete(friendId)
        );
    }
    
    private void OnInputComplete(string friendId)
    {
        // 发送添加好友请求
        SendAddFriendRequest(friendId);
    }
}
```

### 6. Tab 切换通信

#### 场景 6.1：主界面 Tab 切换

```csharp
// 主界面
public class MainUI : UIWindow
{
    private void OnTabClick(int tabIndex)
    {
        // 通知所有子面板 Tab 切换
        GameEvent.Send("OnMainTabChanged", tabIndex);
    }
}

// 子面板 1：角色面板
public class CharacterUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<int>("OnMainTabChanged", OnTabChanged);
    }
    
    private void OnTabChanged(int tabIndex)
    {
        // 如果切换到角色 Tab，显示自己
        this.Visible = (tabIndex == 0);
    }
}

// 子面板 2：背包面板
public class BagUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<int>("OnMainTabChanged", OnTabChanged);
    }
    
    private void OnTabChanged(int tabIndex)
    {
        // 如果切换到背包 Tab，显示自己
        this.Visible = (tabIndex == 1);
        
        if (this.Visible)
        {
            // 刷新背包数据
            RefreshBagData();
        }
    }
}
```

### 7. 拖拽系统通信

#### 场景 7.1：物品拖拽

```csharp
// 拖拽管理器
public class DragManager : MonoBehaviour
{
    private static DragManager instance;
    private object draggingData;
    
    public static void BeginDrag(object data)
    {
        instance.draggingData = data;
        GameEvent.Send("OnDragBegin", data);
    }
    
    public static void EndDrag()
    {
        GameEvent.Send("OnDragEnd", instance.draggingData);
        instance.draggingData = null;
    }
}

// 背包格子（拖拽源）
public class BagSlot : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        DragManager.BeginDrag(itemData);
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        DragManager.EndDrag();
    }
}

// 装备槽（拖拽目标）
public class EquipSlot : MonoBehaviour, IDropHandler
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<object>("OnDragBegin", OnDragBegin);
        GameEvent.AddEventListener<object>("OnDragEnd", OnDragEnd);
    }
    
    private void OnDragBegin(object data)
    {
        if (data is ItemData item && CanEquip(item))
        {
            // 高亮显示可装备
            SetHighlight(true);
        }
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // 装备物品
        EquipItem(itemData);
        
        // 通知背包刷新
        GameEvent.Send("OnItemEquipped", itemData);
    }
}
```

### 8. 红点系统通信

#### 场景 8.1：红点状态变化

```csharp
// 红点管理器
public class RedDotManager
{
    public static void UpdateRedDot(string path, bool hasRedDot)
    {
        // 通知所有监听该路径的 UI
        GameEvent.Send("OnRedDotChanged", path, hasRedDot);
    }
}

// 主界面按钮
public class MainUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<string, bool>("OnRedDotChanged", OnRedDotChanged);
    }
    
    private void OnRedDotChanged(string path, bool hasRedDot)
    {
        // 更新对应按钮的红点
        if (path == "Main/Bag")
        {
            btnBag.GetComponent<RedDotView>().SetActive(hasRedDot);
        }
        else if (path == "Main/Mail")
        {
            btnMail.GetComponent<RedDotView>().SetActive(hasRedDot);
        }
    }
}

// 背包面板
public class BagUI : UIWindow
{
    protected override void OnCreate()
    {
        GameEvent.AddEventListener<string, bool>("OnRedDotChanged", OnRedDotChanged);
    }
    
    private void OnRedDotChanged(string path, bool hasRedDot)
    {
        // 更新背包内部的红点
        if (path.StartsWith("Main/Bag/"))
        {
            UpdateBagTabRedDot(path, hasRedDot);
        }
    }
}
```

### 9. 提示/Toast 通信

#### 场景 9.1：全局提示

```csharp
// 任何地方触发提示
public class BagUI : UIWindow
{
    private void OnUseItem(ItemData item)
    {
        // 使用物品
        bool success = UseItem(item);
        
        if (success)
        {
            // 显示提示
            GameEvent.Send("ShowToast", $"使用了 {item.Name}");
        }
    }
}

// Toast 管理器
public class ToastManager : MonoBehaviour
{
    private void Start()
    {
        GameEvent.AddEventListener<string>("ShowToast", ShowToast);
    }
    
    private void ShowToast(string message)
    {
        // 显示 Toast
        var toast = Instantiate(toastPrefab);
        toast.GetComponent<Text>().text = message;
        // 播放动画...
    }
}
```

### 10. 战斗 UI 通信

#### 场景 10.1：技能冷却同步

```csharp
// 技能栏
public class SkillBarUI : UIWindow
{
    private void OnSkillCast(int skillId)
    {
        // 通知所有技能槽更新冷却
        GameEvent.Send("OnSkillCooldownStart", skillId, cooldownTime);
    }
}

// 技能槽
public class SkillSlot : MonoBehaviour
{
    private void Start()
    {
        GameEvent.AddEventListener<int, float>("OnSkillCooldownStart", OnCooldownStart);
    }
    
    private void OnCooldownStart(int skillId, float cooldownTime)
    {
        if (this.skillId == skillId)
        {
            // 开始冷却动画
            StartCooldown(cooldownTime);
        }
    }
}
```

## 二、通信方式对比

### 方式 1：TEngine 事件（推荐用于平级/全局通信）

```csharp
// 优点：解耦、支持多订阅者
// 缺点：需要手动管理订阅/取消订阅

// 发送
GameEvent.Send("OnItemSelected", itemData);

// 接收
GameEvent.AddEventListener<ItemData>("OnItemSelected", OnItemSelected);
```

### 方式 2：直接传参（推荐用于父子通信）

```csharp
// 优点：简单直接、类型安全
// 缺点：强耦合

// 打开 UI 并传参
GameModule.UI.ShowUIAsync<DetailUI>(itemData, selectedTab);

// 接收参数
protected override void OnRefresh(params object[] userDatas)
{
    ItemData item = userDatas[0] as ItemData;
}
```

### 方式 3：回调函数（推荐用于对话框）

```csharp
// 优点：明确的调用关系
// 缺点：只能一对一

// 传递回调
GameModule.UI.ShowUIAsync<ConfirmDialogUI>(
    "提示",
    "确认删除？",
    () => OnConfirm(),
    () => OnCancel()
);
```

### 方式 4：单例管理器（推荐用于全局状态）

```csharp
// 优点：全局访问、状态集中管理
// 缺点：可能导致过度耦合

// 通过管理器通信
RedDotManager.Instance.UpdateRedDot("Main/Bag", true);
DragManager.Instance.BeginDrag(itemData);
```

## 三、最佳实践

### 1. 选择合适的通信方式

| 场景 | 推荐方式 | 原因 |
|------|---------|------|
| 父子窗口 | 直接传参 | 简单直接 |
| 平级窗口 | TEngine 事件 | 解耦 |
| 全局通知 | TEngine 事件 | 多订阅者 |
| 对话框回调 | 回调函数 | 明确关系 |
| 全局状态 | 单例管理器 | 集中管理 |

### 2. 事件命名规范

```csharp
// ✅ 好的命名
"OnItemSelected"        // 清晰的动作
"OnCurrencyChanged"     // 明确的状态变化
"OnBagItemCellClick"    // 具体的来源

// ❌ 不好的命名
"ItemEvent"             // 太模糊
"Update"                // 太通用
"Event1"                // 无意义
```

### 3. 避免内存泄漏

```csharp
// ✅ 正确：在 OnDestroy 中取消订阅
protected override void OnCreate()
{
    GameEvent.AddEventListener("OnItemSelected", OnItemSelected);
}

protected override void OnDestroy()
{
    GameEvent.RemoveEventListener("OnItemSelected", OnItemSelected);
}

// ❌ 错误：忘记取消订阅
protected override void OnCreate()
{
    GameEvent.AddEventListener("OnItemSelected", OnItemSelected);
}
// 窗口销毁后事件仍然订阅，导致内存泄漏
```

### 4. 避免过度通信

```csharp
// ❌ 不好：频繁发送事件
void Update()
{
    GameEvent.Send("OnMouseMove", Input.mousePosition); // 每帧发送
}

// ✅ 好：只在必要时发送
void OnMouseClick()
{
    GameEvent.Send("OnItemClick", itemData); // 点击时发送
}
```

## 四、总结

UI 组件间通信的核心原则：

1. **父子通信** → 直接传参
2. **平级通信** → TEngine 事件
3. **全局通知** → TEngine 事件
4. **对话框回调** → 回调函数
5. **全局状态** → 单例管理器

记住：**TEngine 事件用于 UI 层，ET 事件用于逻辑层**！
