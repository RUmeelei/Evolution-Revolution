using System;
using UnityEngine;
using System.Collections.Generic;

namespace ER
{
    namespace Unit
    {
        public enum FormationType
        {
            None = 0,
            Column = 1,
            LooseLine = 2,
            CloseOrder = 3,
            Wedge = 4,
            Square = 5,
        }

        public enum TacticalCondition
        {
            Always = 0,
            OnMarch = 1,
            UnderRangedFire = 2,
            InMelee = 3,
            Outnumbered = 4,
            LowHealthOrMorale = 5,
        }

        public enum TacticalAction
        {
            AdvanceToObjective = 0,
            SkirmishKeepDistance = 1,
            ChargeMelee = 2,
            HoldPosition = 3,
            RetreatToSafety = 4,
        }

        [Serializable]
        public class TacticalRule
        {
            public string RuleName;
            public bool IsEnabled = true;

            public TacticalCondition Condition;
            public float ParameterValue;

            public TacticalAction Action;
            public FormationType Formation;

            public TacticalRule(string name, TacticalCondition condition, float param, TacticalAction action, FormationType formation)
            {
                RuleName = name;
                IsEnabled = true;

                Condition = condition;
                ParameterValue = param;

                Action = action;
                Formation = formation;
            }
        }

        [Serializable]
        public class TacticalDoctrine
        {
            public string DoctrineName = "Standard Historical Doctrine";
            public List<TacticalRule> Rules = new List<TacticalRule>();

            private static Dictionary<string, TacticalDoctrine> doctrinesByCulture = new Dictionary<string, TacticalDoctrine>();

            public static TacticalDoctrine GetOrCreateDoctrine(string cultureId)
            {
                if (string.IsNullOrEmpty(cultureId)) return CreateDefaultDoctrine();

                if (!doctrinesByCulture.TryGetValue(cultureId, out var doc))
                {
                    doc = CreateDefaultDoctrine();
                    doctrinesByCulture[cultureId] = doc;
                }

                return doc;
            }

            public static TacticalDoctrine CreateDefaultDoctrine()
            {
                var doc = new TacticalDoctrine();
                
                doc.Rules.Add(new TacticalRule("Retreat if critically wounded", TacticalCondition.LowHealthOrMorale, 0.30f, TacticalAction.RetreatToSafety, FormationType.Column));
                doc.Rules.Add(new TacticalRule("Retreat if outnumbered by 4+", TacticalCondition.Outnumbered, 4f, TacticalAction.RetreatToSafety, FormationType.Square));
                doc.Rules.Add(new TacticalRule("Close order in melee combat", TacticalCondition.InMelee, 1.8f, TacticalAction.ChargeMelee, FormationType.CloseOrder));
                doc.Rules.Add(new TacticalRule("Loose line in ranged skirmish", TacticalCondition.UnderRangedFire, 5.0f, TacticalAction.SkirmishKeepDistance, FormationType.LooseLine));
                doc.Rules.Add(new TacticalRule("March in column to objective", TacticalCondition.OnMarch, 0f, TacticalAction.AdvanceToObjective, FormationType.Column));

                return doc;
            }

            public TacticalRule Evaluate(Unit unit, int nearbyAlliesCount, int nearbyEnemiesCount, float nearestEnemyDist)
            {
                for (int i = 0; i < Rules.Count; i++)
                {
                    var rule = Rules[i];

                    if (!rule.IsEnabled) continue;

                    switch (rule.Condition)
                    {
                        case TacticalCondition.LowHealthOrMorale:
                            if (unit.GetCurrentHealthPercent() <= rule.ParameterValue) return rule;
                            break;

                        case TacticalCondition.Outnumbered:
                            if (nearbyEnemiesCount > 0 && (nearbyEnemiesCount - nearbyAlliesCount) >= rule.ParameterValue) return rule;
                            break;

                        case TacticalCondition.InMelee:
                            if (nearestEnemyDist <= rule.ParameterValue) return rule;
                            break;

                        case TacticalCondition.UnderRangedFire:
                            if (nearestEnemyDist > 1.8f && nearestEnemyDist <= rule.ParameterValue) return rule;
                            break;

                        case TacticalCondition.OnMarch:
                            if (nearbyEnemiesCount == 0) return rule;
                            break;

                        case TacticalCondition.Always:
                            return rule;
                    }
                }

                return Rules[Rules.Count - 1];
            }

            public static Vector2 GetFormationSlotOffset(int slotIndex, int totalUnits, Vector2 forwardDir, FormationType formation)
            {
                if (slotIndex <= 0 || totalUnits <= 1 || formation == FormationType.None) return Vector2.zero;
            
                if (forwardDir.sqrMagnitude < 0.001f) forwardDir = Vector2.up;
                forwardDir.Normalize();
            
                Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
                
                float scale = Core.CoreManager.MainConfig != null ? Core.CoreManager.MainConfig.UnitSeparationRadius / 0.35f : 0.28f;
            
                switch (formation)
                {
                    case FormationType.Column:
                    {
                        int col = (slotIndex % 2 == 0) ? -1 : 1;
                        int row = slotIndex / 2;
            
                        return ((rightDir * (col * 0.28f)) - (forwardDir * (row * 0.55f))) * scale;
                    }
            
                    case FormationType.CloseOrder:
                    {
                        int width = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(totalUnits) * 1.4f), 3, 10);
                        int row = slotIndex / width;
                        int colIndex = slotIndex % width;
                        float centeredCol = colIndex - (width - 1) * 0.5f;
            
                        return ((rightDir * (centeredCol * 0.35f)) - (forwardDir * (row * 0.40f))) * scale;
                    }
            
                    case FormationType.LooseLine:
                    {
                        int width = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(totalUnits) * 1.6f), 3, 10);
                        int row = slotIndex / width;
                        int colIndex = slotIndex % width;
                        float centeredCol = colIndex - (width - 1) * 0.5f;
            
                        return ((rightDir * (centeredCol * 1.25f)) - (forwardDir * (row * 1.0f))) * scale;
                    }
            
                    case FormationType.Wedge:
                    {
                        int row = Mathf.FloorToInt(Mathf.Sqrt(slotIndex));
                        int posInRow = slotIndex - (row * row);
                        float centeredCol = posInRow - row;
            
                        return ((rightDir * (centeredCol * 0.45f)) - (forwardDir * (row * 0.45f))) * scale;
                    }
            
                    case FormationType.Square:
                    {
                        float angle = (slotIndex / (float)Mathf.Max(1, totalUnits - 1)) * Mathf.PI * 2f;
                        float radius = Mathf.Clamp(totalUnits * 0.12f, 0.45f, 1.8f);
            
                        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (scale * radius);
                    }
            
                    default:
                        return Vector2.zero;
                }
            }
        }
    }
}