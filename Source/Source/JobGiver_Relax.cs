using System;
using System.Collections.Generic;
using System.Linq;
using Hospitality.Utilities;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Hospitality;

public class JobGiver_Relax : ThinkNode_JobGiver
{
    private DefMap<JoyGiverDef, float> joyGiverChances;

    public override float GetPriority(Pawn pawn)
    {
        if (pawn == null)
        {
            Log.ErrorOnce("pawn == null", 745743);
            return 0f;
        }

        if (pawn.needs?.joy == null)
        {
            // Apparently there are guests without joy...
            return 0f;
        }

        var curLevel = pawn.needs.joy.CurLevel;

        if (curLevel < 0.35f)
        {
            return 6f;
        }

        if (curLevel < 0.9f)
            return 1 - curLevel;
        return 0f;
    }

    public override void ResolveReferences()
    {
        joyGiverChances = new DefMap<JoyGiverDef, float>();
    }

    public override Job TryGiveJob(Pawn pawn)
    {
        if (pawn == null)
        {
            Log.ErrorOnce("pawn == null", 987272);
            return null;
        }

        if (pawn.CurJob != null)
        {
            //Log.ErrorOnce(pawn.NameStringShort+ " already has a job: "+pawn.CurJob, 4325+pawn.thingIDNumber);
            return pawn.CurJob;
        }

        if (pawn.needs == null) Log.ErrorOnce(pawn.LabelShort + " has no needs", 3463 + pawn.thingIDNumber);
        if (pawn.needs.joy == null) Log.ErrorOnce(pawn.LabelShort + " has no joy need", 8585 + pawn.thingIDNumber);
        if (pawn.skills == null) Log.ErrorOnce(pawn.LabelShort + " has no skills", 22352 + pawn.thingIDNumber);
        if (pawn.GetTimeAssignment() == null) Log.ErrorOnce(pawn.LabelShort + " has no time assignments", 74564 + pawn.thingIDNumber);

        var allDefsListForReading = PopulateChances(pawn); // Moved to own function
        if (GetJob(pawn, allDefsListForReading, out var job)) return job;
        //Log.ErrorOnce(pawn.LabelShort + " did not get a relax job.", 45745 + pawn.thingIDNumber);
        CheckArea(pawn);
        return null;
    }

    private bool GetJob(Pawn pawn, List<JoyGiverDef> allDefsListForReading, out Job job)
    {
        for (var j = 0; j < joyGiverChances.Count; j++)
        {
            try
            {
                if (!allDefsListForReading.TryRandomElementByWeight(d => joyGiverChances[d], out var giverDef))
                {
                    //Log.ErrorOnce($"{pawn.LabelShort} could not find a joygiver. DefsCount = {allDefsListForReading.Count}", 45747 + pawn.thingIDNumber);
                    break;
                }

                job = giverDef?.Worker?.TryGiveJob(pawn);
                if (job != null)
                {
                    return true;
                }

                joyGiverChances[giverDef] = 0f;
            }
            catch (Exception e)
            {
                string allDefs;
                try
                {
                    allDefs = allDefsListForReading?.Select(d => d?.defName).ToCommaList();
                }
                catch
                {
                    allDefs = "failed to parse";
                }

                Log.Error($"{pawn.LabelShort} failed to get a relax job. allDefs: {allDefs}\n{e.Message}");
            }
        }

        job = null;
        return false;
    }

    private static void CheckArea(Pawn pawn)
    {
        var area = pawn.GetGuestArea();
        //if (area == null)
        //{
        //    Log.ErrorOnce(pawn.LabelShort + " has a null area!", 932463 + pawn.thingIDNumber);
        //    return;
        //}

        if (area is { TrueCount: 0 })
        {
            Log.ErrorOnce(pawn.LabelShort + " has an area that is empty!", 43737 + pawn.thingIDNumber);
        }
    }

    private List<JoyGiverDef> PopulateChances(Pawn pawn)
    {
        // From Core
        var allDefsListForReading = DefDatabase<JoyGiverDef>.AllDefsListForReading;

        // ADDED
        if (allDefsListForReading == null)
        {
            Log.Message("AllDefsListForReading == null");
            return new List<JoyGiverDef>();
        } // ^^^

        var tolerances = pawn.needs.joy.tolerances;
        foreach (var joyGiverDef in allDefsListForReading)
        {
            joyGiverChances[joyGiverDef] = 0f;

            //if (this.JoyGiverAllowed(joyGiverDef)) REMOVED
            if (pawn.needs.joy != null) // ADDED
            {
                if (!pawn.needs.joy.tolerances.BoredOf(joyGiverDef.joyKind))
                {
                    if (joyGiverDef.Worker.CanBeGivenTo(pawn))
                    {
                        if (joyGiverDef.pctPawnsEverDo < 1f)
                        {
                            Rand.PushState(pawn.thingIDNumber ^ 63216713);
                            if (Rand.Value >= joyGiverDef.pctPawnsEverDo)
                            {
                                Rand.PopState();
                                goto IL_FB;
                            }

                            Rand.PopState();
                        }

                        var tolerance = tolerances[joyGiverDef.joyKind];
                        var factor = Mathf.Pow(1f - tolerance, 5f);
                        factor = Mathf.Max(0.001f, factor);
                        joyGiverChances[joyGiverDef] = joyGiverDef.Worker.GetChance(pawn) * factor;
                    }
                }
            }

            IL_FB: ;
        }

        return allDefsListForReading;
    }
}