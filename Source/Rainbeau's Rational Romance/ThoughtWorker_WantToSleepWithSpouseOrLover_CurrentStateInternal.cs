using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RationalRomance_Code;

[HarmonyPatch(typeof(ThoughtWorker_WantToSleepWithSpouseOrLover),
    nameof(ThoughtWorker_WantToSleepWithSpouseOrLover.CurrentStateInternal))]
public static class ThoughtWorker_WantToSleepWithSpouseOrLover_CurrentStateInternal
{
    public static void Postfix(ref ThoughtState __result, Pawn p)
    {
        if (__result.StageIndex == ThoughtState.Inactive.StageIndex)
        {
            return;
        }
        
        //FIXED: Check if they have Polyamorous first since the rest doesn't matter if they don't
        if (!HasPolyamorousTrait(p))
        {
            return;
        }

        var directPawnRelation = LovePartnerRelationUtility.ExistingMostLikedLovePartnerRel(p, false);
        
        var multiplePartners =
            (
                from r in p.relations.PotentiallyRelatedPawns
                where LovePartnerRelationUtility.LovePartnerRelationExists(p, r)
                select r
            ).Count() > 1;

        // FIXED: Check for null bed BEFORE calling GetRoom() on it
        if (directPawnRelation == null || p.ownership?.OwnedBed == null)
        {
            return;
        }

        var room = p.ownership.OwnedBed.GetRoom();
        if (room == null)
        {
            return;
        }

        var partnerBedInRoom = (
            from t in room.ContainedBeds
            where
                t?.OwnersForReading != null
                && t.OwnersForReading.Contains(directPawnRelation.otherPawn)
            select t
        ).Any();

        if (multiplePartners && partnerBedInRoom)
        {
            __result = ThoughtState.Inactive;
        }
    }

    private static bool HasPolyamorousTrait(Pawn pawn)
    {
        return pawn.story?.traits?.HasTrait(RRRTraitDefOf.Polyamorous) ?? false;
    }
}
