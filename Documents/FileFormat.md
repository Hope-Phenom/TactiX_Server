# .tactix 文件格式规范

本文档定义战术文件（`.tactix`）的JSON结构和解析规则。

---

## 文件概述

| 属性 | 值 |
|------|-----|
| **扩展名** | `.tactix` |
| **格式** | JSON |
| **编码** | UTF-8 |
| **最大大小** | 根据用户等级（10MB-100MB） |

---

## JSON结构定义

### 完整结构

```json
{
  "name": "string?",
  "author": "string?",
  "description": "string?",
  "version": "string?",
  "race": "string?",
  "actions": [
    {
      "itemAbbr": "string",
      "time": "int",
      "supply": "string?",
      "notes": "string?"
    }
  ]
}
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `name` | string | 否 | 战术名称，最长255字符 |
| `author` | string | 否 | 作者名称，最长128字符 |
| `description` | string | 否 | 战术描述 |
| `version` | string | 否 | 文件版本号（非系统版本） |
| `race` | string | 否 | 种族代码（P/T/Z），可选，系统会自动识别 |
| `actions` | array | **是** | 战术步骤数组，至少1个元素 |

---

## actions 数组

### Action 对象结构

```json
{
  "itemAbbr": "string",
  "time": "int",
  "supply": "string?",
  "notes": "string?"
}
```

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `itemAbbr` | string | **是** | 单位/建筑缩写（用于种族识别） |
| `time` | int | **是** | 执行时间（游戏内秒数） |
| `supply` | string | 否 | 人口状态（如"13/15"） |
| `notes` | string | 否 | 备注/说明 |

### itemAbbr 命名规则

`itemAbbr` 通常以种族首字母开头：

| 种族 | 前缀 | 示例 |
|------|------|------|
| 神族 (P) | P | `Pylon`, `Probe`, `Gateway` |
| 人族 (T) | T | `Barracks`, `SCV`, `Marine` |
| 虫族 (Z) | Z | `Zergling`, `SpawningPool`, `Drone` |

> **注意：** 部分单位/建筑缩写可能不以种族首字母开头（如人族的 `SupplyDepot`），系统会继续检查后续 action 或使用 `race` 字段。

---

## 示例文件

### 神族战术 (P)

```json
{
  "name": "PvZ 4Gate Rush",
  "author": "ProtossMaster",
  "description": "经典4兵营快攻战术，适合对抗虫族",
  "version": "1.0",
  "actions": [
    { "itemAbbr": "Pylon", "time": 0, "supply": "9/10", "notes": "第一个水晶塔" },
    { "itemAbbr": "Gateway", "time": 135, "supply": "12/16", "notes": "第一兵营" },
    { "itemAbbr": "Assimilator", "time": 165, "supply": "13/16", "notes": "气矿" },
    { "itemAbbr": "Pylon", "time": 200, "supply": "14/16", "notes": "第二个水晶塔" },
    { "itemAbbr": "Gateway", "time": 230, "supply": "16/20", "notes": "第二兵营" },
    { "itemAbbr": "Gateway", "time": 240, "supply": "17/20", "notes": "第三兵营" },
    { "itemAbbr": "Gateway", "time": 250, "supply": "18/20", "notes": "第四兵营" },
    { "itemAbbr": "Zealot", "time": 300, "supply": "20/24", "notes": "开始生产狂热者" }
  ]
}
```

**种族识别：** 第一个 `itemAbbr` = "Pylon"，首字符 'P' → 神族

---

### 人族战术 (T)

```json
{
  "name": "TvZ 3Rax Bio Push",
  "author": "TerranPlayer",
  "description": "3兵营生化部队推进",
  "actions": [
    { "itemAbbr": "SupplyDepot", "time": 0, "supply": "9/10", "notes": "第一个补给站" },
    { "itemAbbr": "Barracks", "time": 65, "supply": "11/11", "notes": "第一兵营" },
    { "itemAbbr": "Refinery", "time": 100, "supply": "12/13", "notes": "气矿" },
    { "itemAbbr": "OrbitalCommand", "time": 165, "supply": "15/19", "notes": "轨道指挥中心" },
    { "itemAbbr": "Barracks", "time": 180, "supply": "16/19", "notes": "第二兵营" },
    { "itemAbbr": "Barracks", "time": 190, "supply": "17/19", "notes": "第三兵营" },
    { "itemAbbr": "Marine", "time": 230, "supply": "19/23", "notes": "开始生产陆战队员" }
  ]
}
```

> **注意：** 此例中 `SupplyDepot` 和 `Barracks` 不是以 T 开头，系统会使用其他识别方式或依赖 `race` 字段。

---

### 虫族战术 (Z)

```json
{
  "name": "ZvT Roach Rush",
  "author": "ZergKing",
  "description": "蟑螂快攻战术",
  "actions": [
    { "itemAbbr": "SpawningPool", "time": 0, "supply": "9/10", "notes": "孵化池" },
    { "itemAbbr": "Extractor", "time": 100, "supply": "11/10", "notes": "气矿" },
    { "itemAbbr": "Queen", "time": 180, "supply": "14/14", "notes": "皇后" },
    { "itemAbbr": "Zergling", "time": 200, "supply": "15/14", "notes": "几只狗侦查" },
    { "itemAbbr": "RoachWarren", "time": 220, "supply": "16/14", "notes": "蟑螂巢" },
    { "itemAbbr": "Roach", "time": 280, "supply": "18/22", "notes": "开始生产蟑螂" }
  ]
}
```

**种族识别：** 第一个 `itemAbbr` = "SpawningPool"，首字符 'S' 无法识别，系统会继续检查后续 action 或使用 `race` 字段。

---

## 解析规则

### 种族自动识别

`TacticsFileParser.DetectRaceFromActions()` 方法：

```
1. 遍历 actions 数组
2. 检查每个 action 的 itemAbbr 字段
3. 取第一个 itemAbbr 的首字符（大写）
4. 匹配规则：
   - 'P' → Protoss (神族)
   - 'T' → Terran (人族)
   - 'Z' → Zerg (虫族)
   - 其他 → null (无法识别)
```

### 元数据提取

```
1. name: 战术名称（显示用）
2. author: 作者名称（显示用）
3. race: 种族代码（P/T/Z）
```

---

## 验证层次

文件上传时经过5层验证：

| 层次 | 验证内容 | 失败条件 |
|------|----------|----------|
| 1. 结构验证 | JSON格式正确 | 非法JSON语法 |
| 2. 大小验证 | 不超过等级限制 | 文件大小超限 |
| 3. 内容验证 | 必需字段存在 | 无 `actions` 数组 |
| 4. XSS验证 | 无危险脚本 | 包含 `<script>` 等危险标签 |
| 5. 哈希验证 | 计算SHA256 | - |

---

## 上传流程

```
客户端选择 .tactix 文件
         │
         ▼
┌─────────────────────┐
│  FileSecurityValidator │
│  5层验证            │
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│  TacticsFileParser  │
│  解析元数据          │
│  - name             │
│  - author           │
│  - race             │
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│  PermissionService  │
│  权限检查            │
│  - CanUpload        │
│  - 每日限制          │
│  - 文件总数限制      │
└─────────────────────┘
         │
         ▼
┌─────────────────────┐
│  保存文件            │
│  - 生成ShareCode    │
│  - 写入数据库        │
│  - status=pending   │
└─────────────────────┘
         │
         ▼
    返回配装码
```

---

## 客户端实现建议

### 文件选择

```javascript
// 推荐文件选择器配置
<input type="file" accept=".tactix" />
```

### 上传请求

```javascript
const formData = new FormData();
formData.append('file', file);
formData.append('Changelog', '初始版本');

const response = await fetch('/api/TacticsHall/Upload', {
  method: 'POST',
  headers: {
    'Authorization': `Bearer ${token}`
  },
  body: formData
});
```

### 文件生成

客户端生成 `.tactix` 文件时：

```javascript
const tacticsData = {
  name: "我的战术",
  author: "PlayerName",
  description: "战术描述",
  actions: [
    { itemAbbr: "Pylon", time: 0, supply: "9/10", notes: "起手" }
  ]
};

const blob = new Blob([JSON.stringify(tacticsData, null, 2)], { type: 'application/json' });
const file = new File([blob], 'tactics.tactix', { type: 'application/json' });
```

---

## 相关文档

- [API_TacticsHall.md](API_TacticsHall.md) - 战术大厅接口
- [Constants.md](Constants.md) - 种族代码定义