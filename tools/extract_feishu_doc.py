import re
import pathlib

ROOT = pathlib.Path(__file__).resolve().parents[1]
HTML = ROOT / "tools" / "_feishu_raw.html"
OUT_MD = ROOT / "doc" / "【末日之王】开播指引.md"
OUT_TXT = ROOT / "doc" / "【末日之王】开播指引_原文摘录.txt"

html = HTML.read_text(encoding="utf-8", errors="ignore")
texts = re.findall(r'data-string="true"[^>]*>([^<]*)</span>', html)
seen: list[str] = []
for raw in texts:
    t = raw.strip().replace("\u200b", "")
    if t and t not in seen:
        seen.append(t)

OUT_TXT.write_text("\n".join(seen), encoding="utf-8")

# Structured markdown from SSR + fetch (best effort)
lines = [
    "# 【末日之王】开播指引",
    "",
    "> 来源：https://smo16lahjs.feishu.cn/docx/QuRKdCwDzomMeCxCHipcXYt1ndh",
    "> 抓取时间：自动导出（SSR 公开页，可能不完整；需登录部分请在飞书内「导出为 Word/Markdown」补全）",
    "",
    "## 更新记录与预告",
    "",
    "| 版本号 | 更新时间 | 更新记录 | 备注 |",
    "| --- | --- | --- | --- |",
    "| 1.0.9 | 2026年4月26日 | 内测上线 | |",
    "",
    "## 一、关于末日之王",
    "",
    "### 一句话介绍",
    "",
    "丧尸入侵！人类退无可退，奋起反抗！抖音首款人类 vs 丧尸 3D 对战玩法！",
    "",
    "### 玩法",
    "",
    "玩家加入不同阵营，召唤单位，以击败对方基地为胜利目标。",
    "",
    "### 玩家操作",
    "",
    "输入 **【1】、【2】、【3】** 加入蓝军、绿军、丧尸；点赞或赠送礼物召唤单位开启战斗。",
    "",
    "### 故事背景",
    "",
    "末日降临，丧尸围城：可带领人类联军抵御丧尸狂潮，或加入丧尸摧毁人类最后的希望，亦或背叛联盟一统世界，去寻找末日的道路。",
    "",
    "## 玩法四大特色",
    "",
    "### 特色一：零成本造画面",
    "",
    "- 零成本卡画面吸量",
    "- 丧尸疯狂爆兵，点赞出超大巨人",
    "- 核武打击倒计时，提供玩家停留时长",
    "",
    "### 特色二：跨时代美术",
    "",
    "- 超写实造景，超大地图，轻松操控",
    "- 超多单位，人 vs 丧尸，极致效果",
    "- 超爆炸特效，全网唯一，玩法巅峰",
    "",
    "### 特色三：3 阵营 + 叛变玩法",
    "",
    "- 人类分为蓝军、绿军，共同迎战丧尸",
    "- 人类击败丧尸后，一方概率叛变，联手丧尸称霸",
    "",
    "### 特色四：丧尸感染",
    "",
    "- 独创新机制：丧尸可感染人类单位",
    "- 节目效果拉满，解说更有梗",
    "",
    "## 文档目录（飞书内章节）",
    "",
    "以下章节标题来自页面结构，正文需在飞书登录后导出补全：",
    "",
]
for t in seen:
    if t in (
        "【末日之王】开播指引",
        "玩法介绍（重要）",
        "直播话术",
        "直播素材",
        "玩法规则",
        "主播引导",
        "关于风响互娱",
        "更新记录与预告",
        "版本号",
        "更新时间",
        "更新记录",
        "备注",
        "1.0.9",
        "2026年4月26日",
        "内测上线",
        "一、关于末日之王",
        "一句话介绍",
        "玩法",
        "玩家",
        "故事背景",
        "游戏视频",
        "玩法四大特色！！！",
        "【末日之王】直播话术",
        "特色一：零成本造画面！",
        "特色二：跨时代美术！",
        "特色三：3阵营 + 叛变玩法！",
        "特色四：丧尸感染！",
    ):
        continue
    if len(t) > 4:
        continue
    lines.append(f"- {t}")

lines.extend(
    [
        "",
        "## 附录：SSR 全文摘录",
        "",
        "```text",
        *seen,
        "```",
    ]
)

OUT_MD.write_text("\n".join(lines), encoding="utf-8")
print(f"Wrote {OUT_MD} ({len(seen)} text nodes)")
