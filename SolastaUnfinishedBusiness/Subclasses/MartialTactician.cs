﻿using JetBrains.Annotations;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Api.Helpers;
using SolastaUnfinishedBusiness.Behaviors;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Interfaces;
using SolastaUnfinishedBusiness.Properties;
using SolastaUnfinishedBusiness.Subclasses.Builders;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;

namespace SolastaUnfinishedBusiness.Subclasses;

[UsedImplicitly]
public sealed class MartialTactician : AbstractSubclass
{
    internal const string Name = "MartialTactician";
    internal const string TacticalAwareness = "TacticalAwareness";

    private static int _gambitPoolIncreases;

    public MartialTactician()
    {
        // kept for backward compatibility
        _ = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetTacticianHonedCraft")
            .SetGuiPresentationNoContent(true)
            .SetFeatureSet(
                FeatureDefinitionPointPoolBuilder
                    .Create("PointPoolTacticianSharpMindExpertise")
                    .SetGuiPresentationNoContent(true)
                    .SetPool(HeroDefinitions.PointsPoolType.Expertise, 1)
                    .AddToDB())
            .AddToDB();

        // kept for backward compatibility
        _ = FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetTacticianBattleClarity")
            .SetGuiPresentationNoContent(true)
            .SetMode(FeatureDefinitionFeatureSet.FeatureSetMode.Exclusion)
            .SetFeatureSet(
                FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinityCreedOfMisaye,
                FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinityCreedOfPakri,
                FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinityCreedOfMaraike,
                FeatureDefinitionSavingThrowAffinitys.SavingThrowAffinityCreedOfSolasta)
            .AddToDB();

        var unlearn = BuildUnlearn();
        var initialPool = BuildGambitPoolIncrease(4, Name);
        var gambitPoolIncrease = BuildGambitPoolIncrease(2, "ImproviseStrategy");
        
        initialPool.GuiPresentation.hidden = true;

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass,
                Sprites.GetSprite(Name, Resources.MartialTactician, 256))
            .AddFeaturesAtLevel(3, BuildSharpMind(), initialPool,
                GambitsBuilders.Learn3Gambit,
                GambitsBuilders.GambitPool)
            .AddFeaturesAtLevel(7, BuildGambitPoolIncrease(),
                GambitsBuilders.Learn2Gambit,
                unlearn)
            .AddFeaturesAtLevel(10, BuildGambitDieSize(DieType.D10),
                gambitPoolIncrease,
                unlearn)
            .AddFeaturesAtLevel(15, BuildGambitPoolIncrease(),
                GambitsBuilders.Learn2Gambit,
                unlearn)
            .AddFeaturesAtLevel(18, BuildTacticalAwareness(), BuildGambitDieSize(DieType.D12),
                unlearn)
            .AddToDB();

        GambitsBuilders.BuildGambits();
    }

    internal override CharacterClassDefinition Klass => CharacterClassDefinitions.Fighter;

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceFighterMartialArchetypes;

    internal override DeityDefinition DeityDefinition => null;

    private static FeatureDefinitionFeatureSet BuildSharpMind()
    {
        return FeatureDefinitionFeatureSetBuilder
            .Create("FeatureSetTacticianSharpMind")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(
                FeatureDefinitionPointPoolBuilder
                    .Create("PointPoolTacticianSharpMindSkill")
                    .SetGuiPresentationNoContent()
                    .SetPool(HeroDefinitions.PointsPoolType.Skill, 1)
                    .AddToDB())
            .AddToDB();
    }

    private static FeatureDefinitionPowerUseModifier BuildGambitPoolIncrease()
    {
        return FeatureDefinitionPowerUseModifierBuilder
            .Create($"PowerUseModifierTacticianGambitPool{_gambitPoolIncreases++:D2}")
            .SetGuiPresentation("PowerUseModifierTacticianGambitPool", Category.Feature)
            .SetFixedValue(GambitsBuilders.GambitPool, 1)
            .AddToDB();
    }

    internal static FeatureDefinition BuildGambitPoolIncrease(int number, string name)
    {
        return FeatureDefinitionPowerUseModifierBuilder
            .Create($"PowerUseModifierTacticianGambitPool{name}")
            .SetGuiPresentation("PowerUseModifierTacticianGambitPool", Category.Feature)
            .SetFixedValue(GambitsBuilders.GambitPool, number)
            .AddToDB();
    }

    private static FeatureDefinitionCustomInvocationPool BuildUnlearn()
    {
        return CustomInvocationPoolDefinitionBuilder
            .Create("InvocationPoolGambitUnlearn")
            .SetGuiPresentation(Category.Feature)
            .Setup(InvocationPoolTypeCustom.Pools.Gambit, 1, true)
            .AddToDB();
    }

    private static FeatureDefinition BuildGambitDieSize(DieType size)
    {
        //doesn't do anything, just to display to player dice size progression on level up
        return FeatureDefinitionBuilder
            .Create($"FeatureTacticianGambitDieSize{size}")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();
    }

    private static FeatureDefinition BuildTacticalAwareness()
    {
        var feature = FeatureDefinitionBuilder
            .Create("FeatureSetTacticianTacticalAwareness")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        feature.AddCustomSubFeatures(new CharacterTurnStartListenerTacticalAwareness(feature));

        return feature;
    }

    private sealed class CharacterTurnStartListenerTacticalAwareness(FeatureDefinition featureDefinition)
        : ICharacterTurnStartListener
    {
        public void OnCharacterTurnStarted(GameLocationCharacter character)
        {
            var rulesetCharacter = character.RulesetCharacter;
            var usablePower = PowerProvider.Get(GambitsBuilders.GambitPool, rulesetCharacter);
            var maxUses = rulesetCharacter.GetMaxUsesOfPower(usablePower);

            // cannot call RepayUse() here as a dynamic pool 
            usablePower.remainingUses++;

            if (usablePower.remainingUses > maxUses)
            {
                usablePower.remainingUses = maxUses;
            }
            else
            {
                rulesetCharacter.LogCharacterUsedFeature(featureDefinition);
            }
        }
    }
}
