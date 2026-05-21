using UnityEngine;

// Clase estatica con las formulas matematicas del sistema de combate.
public static class CombatCalculator
{
    public static bool Precision(int characterPrecision, int enemyEvasion, TurnData turnData)
    {
        if (turnData.actionType == CombatAction.Magic)
        {
            if (turnData.selectedMagic.magicType == MagicType.Heal)
                return true;

            float successChance = Mathf.Clamp(
                (characterPrecision + turnData.selectedMagic.precision) / 2f - enemyEvasion + 50f,
                20f, 95f);
            return Dice() <= successChance;
        }

        if (turnData.actionType == CombatAction.Attack)
        {
            float successChance = Mathf.Clamp(
                characterPrecision - enemyEvasion + 50f,
                20f, 95f);
            return Dice() <= successChance;
        }

        return true;
    }

    public static int ActionValue(CharacterStats emitter, CharacterStats receptor, TurnData turnData)
    {
        if (turnData.actionType == CombatAction.Attack)
        {
            // Mitigacion porcentual: DEF / (DEF + 100).
            float mitigacion = receptor.currentDefense / (receptor.currentDefense + 100f);
            int rawDamage = Mathf.RoundToInt(emitter.currentPhysicAtk * (1f - mitigacion));
            float variance = Random.Range(0.9f, 1.1f);
            return Mathf.Max(1, Mathf.RoundToInt(rawDamage * variance));
        }

        if (turnData.actionType == CombatAction.Magic)
        {
            Magic magic = turnData.selectedMagic;
            int magicValor = magic.valor * CharacterStats.StatScale;

            switch (magic.magicType)
            {
                case MagicType.Heal:
                    return (emitter.currentMagicPower + magicValor) / 4;

                case MagicType.Damage:
                    int rawMagicDamage = Mathf.RoundToInt(
                        (emitter.currentMagicPower + magicValor) / 4f - receptor.currentDefense * 0.2f);
                    float variance = Random.Range(0.9f, 1.1f);
                    return Mathf.Max(1, Mathf.RoundToInt(Mathf.Max(1, rawMagicDamage) * variance));
            }
        }

        return 0;
    }

    private static int Dice()
    {
        return Random.Range(1, 101);
    }
}
