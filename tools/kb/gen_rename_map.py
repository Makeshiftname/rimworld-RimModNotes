# -*- coding: utf-8 -*-
"""生成「自建/收集 × 功能」两级分类的重命名映射表（dry-run，不改盘）。

用法（仓库根）:
    python tools/kb/gen_rename_map.py            # 打印完整映射表
    python tools/kb/gen_rename_map.py --json     # 打印 JSON（供后续 git mv 脚本复用）
    python tools/kb/gen_rename_map.py --review   # 只打印需人工确认归属的 mod

规则（用户确认）:
  - 大类文件夹：自建/ 、收集/
  - 收集类保持原相对顺序连续重编 01-82（原 55 拆成 55/56，其后顺延）
  - 自建类从 01 起
  - 目录名 = NN-功能-名称（功能前缀英文小写，与 mod-index Type 一致）
  - 功能归属以 mod-index MANUAL 人工权威清单为准；79/80/81/53 为待复核项
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from kb_common import REPO_ROOT, NUMBER_RE  # noqa: E402

SELF_BUILT = "82-自建-RimTalkPersonaTraits"

# 人工权威功能映射（键=现有目录名；来源 docs/knowledge/mod-index.md MANUAL 类型权威清单）
AUTHORITATIVE_FUNC = {
    "01-AlertUtility": "standalone",
    "02-ItemPolicy": "standalone",
    "03-RecipeBook": "standalone",
    "04-WeatherControlDeviceCont_zh": "translation",
    "05-HighlightEnemies": "standalone",
    "06-ColonyManager_zh": "empty",
    "07-RPP_Bill_AllMech_Patch": "patch",
    "08-RppCommonSenseCleanBeforeCookingPatch": "patch",
    "09-GUILib": "lib",
    "10-QuestItemWatch": "standalone",
    "11-RoadOnIcePatch": "patch",
    "12-RimSavesMoreAutoaveSlotsPatch": "patch",
    "13-SYR_Doormats_zh": "translation",
    "14-SurgeryNeverFail": "standalone",
    "15-MusicalInstrumentsPatch": "patch",
    "16-ResearchPrerequisites": "standalone",
    "17-DeathWeapon": "standalone",
    "18-HakuroXenohumanZh": "translation",
    "19-FacialAnimationEyeFix": "patch",
    "20-DubsMenusRightClickAction": "patch",
    "21-ArmorRacksForceWearFix": "patch",
    "22-MoreMapSeeds": "standalone",
    "23-MoreScenarioSearchbars": "patch",
    "24-DeathIsComing": "standalone",
    "25-AMonkeyExpansion_zh": "translation",
    "26-RestockingStatus": "standalone",
    "27-LetterStackCleaner2": "standalone",
    "28-AllowToolGhoulFix": "patch",
    "29-PermanentUnnaturalDarkness": "standalone",
    "30-SilencedToxifierGenerator": "xml",
    "31-AutomaticBioferriteHarvesting": "standalone",
    "32-PermanentDarknessExtendedDontStarve": "special",
    "33-PolarisblocSecurityForceDistressCallPatch": "patch",
    "34-WorkbenchZone": "standalone",
    "35-QuickDumpWornCloth": "standalone",
    "36-MedOperationsTabWithMedRestrict": "standalone",
    "37-PolarisblocFoodToDrugPatch": "xml",
    "38-DeepStorageContentsTabSearchPatch": "patch",
    "39-MoelotlSimpleSidearmsCompatiblePatch": "patch",
    "40-AreaUnlockerReorderFix": "patch",
    "41-PolarisblocExt": "patch",
    "42-SilentDeathPall": "standalone",
    "43-RatkinCursesStandalone": "standalone",
    "44-NotSoManyPolicies": "standalone",
    "45-TargetLine": "standalone",
    "46-NotSoLoudMilira": "xml",
    "47-WorldMapEditor": "standalone",
    "48-RitualOutcomeFindRelic": "standalone",
    "49-UnlockHarbingerTreeInAllCases": "patch",
    "50-RACurseStandalone": "standalone",
    "51-RACursePatch": "patch",
    "52-ScreamingStarfishForAllStorytellers": "xml",
    "53-AllowTunnelersToDrillFork": "patch",
    "54-AnotherAllowTool": "standalone",
    "55-CommonModCompatibilityPatches": "patch",
    "55-LimitedLetterSlots": "standalone",
    "56-AllowRightClickWorkOutsideZone": "standalone",
    "57-MoreRaidStrategies-HybridRaids": "standalone",
    "58-DraftPawnCanGoToOccupiedCell": "patch",
    "59-GhoulCommands": "standalone",
    "60-MaximumAnimalDensity": "standalone",
    "61-MoreResponsivePlanet": "standalone",
    "62-PawnNotBlockingConstruct": "standalone",
    "63-HideGenebankGenesFromTraders": "patch",
    "64-LandingOnAsteroid": "standalone",
    "65-NewBlueprint": "standalone",
    "66-CaravanShuttleMassInfoInspectString": "standalone",
    "67-DontMeditateYet": "standalone",
    "68-BetterOutfitStand": "standalone",
    "69-RitualOutcomeSelection": "standalone",
    "70-LinuxImeFix": "patch",
    "71-FacialAnimationStatueSnapshotFix": "patch",
    "72-RimLocksmith": "standalone",
    "73-UsefulStats": "standalone",
    "74-GhoulAttackSpin": "standalone",
    "75-SmoothDragSelect": "standalone",
    "76-ColonyGroupsTargetablePortraits": "patch",
    "77-KillingReward": "standalone",
    "78-RimFlixAnimeShows": "xml",
    "79-RimTalk": "standalone",          # MANUAL：启发式误判 patch，实际独立功能
    "80-RimTalkExpandMemory": "patch",   # 待复核
    "81-HautsAddedTraits": "patch",      # 待复核（独立功能+Harmony 混合）
    "82-自建-RimTalkPersonaTraits": "xml",
}

# 需要人工复核归属的目录（功能前缀可能不准，实施前确认）
REVIEW = {"79-RimTalk", "80-RimTalkExpandMemory", "81-HautsAddedTraits", "53-AllowTunnelersToDrillFork"}


def _title_of(dirname: str) -> str:
    """去掉编号前缀与自建标记，返回纯名称。"""
    t = NUMBER_RE.sub("", dirname, count=1).lstrip("-_ ")
    t = re.sub(r"^自建[-_]", "", t)
    return t


def build_map() -> list[dict]:
    # 映射基于静态权威表（旧目录名），不依赖磁盘当前状态；
    # 按旧目录名排序即原编号相对顺序（55-Common < 55-Limited）。
    collected = sorted(
        (n for n in AUTHORITATIVE_FUNC if re.match(r"^\d+[-_]", n) and n != SELF_BUILT),
        key=lambda n: n,
    )
    rows = []
    for new_num, old in enumerate(collected, start=1):
        func = AUTHORITATIVE_FUNC.get(old)
        if func is None:
            raise SystemExit(f"[ERROR] 缺少功能映射: {old}")
        new_name = f"{new_num:02d}-{func}-{_title_of(old)}"
        rows.append({
            "category": "收集",
            "old": old,
            "new_dir": f"收集/{new_name}",
            "func": func,
            "needs_review": old in REVIEW,
        })
    rows.append({
        "category": "自建",
        "old": SELF_BUILT,
        "new_dir": f"自建/01-{AUTHORITATIVE_FUNC[SELF_BUILT]}-{_title_of(SELF_BUILT)}",
        "func": AUTHORITATIVE_FUNC[SELF_BUILT],
        "needs_review": False,
    })
    return rows


def main() -> None:
    rows = build_map()
    if "--json" in sys.argv:
        print(json.dumps(rows, ensure_ascii=False, indent=2))
        return
    if "--review" in sys.argv:
        for r in rows:
            if r["needs_review"]:
                print(f"{r['old']}  ->  {r['new_dir']}   [{r['func']}]  <-- 待复核")
        return
    print(f"收集类 {sum(1 for r in rows if r['category']=='收集')} 个，自建类 {sum(1 for r in rows if r['category']=='自建')} 个\n")
    for r in rows:
        flag = "  <-- 待复核" if r["needs_review"] else ""
        print(f"{r['category']}  {r['old']:55s} -> {r['new_dir']}{flag}")


if __name__ == "__main__":
    main()
