using System.Collections.Generic;
using UnityEngine;

class TutorialManager : Singleton<TutorialManager>
{
    public ETutorial ActiveTutorial = ETutorial.None;

    private List<ETutorial> mCompletedTutorials = new List<ETutorial>();

    public string TutorialToString(ETutorial tutorial)
    {
        switch (tutorial)
        {
        case ETutorial.MovingTheHero                : return "Tutorial_Game02_Movement";
        case ETutorial.UsingAbilities               : return "Tutorial_Game04_Ability";
        case ETutorial.SpawningAllies               : return "Tutorial_Game03_Ally";
        case ETutorial.UsingBowAgainstFlyingEnemies : return "Tutorial_Game05_Flying";
        default:
            UnityEngine.Debug.LogError("Unsupported tutorial type.");
            return "ERROR";
        }
    }

    public void StartTutorial(ETutorial tutorial)
	{
		SingletonMonoBehaviour<TutorialMain>.Instance.StartTutorial(TutorialToString(tutorial));
        ActiveTutorial = tutorial;
	}

    public void FinishActiveTutorial()
    {
        mCompletedTutorials.Add(ActiveTutorial);
        ActiveTutorial = ETutorial.None;
    }

    public bool HasCompleted(ETutorial tutorial)
    {
        return mCompletedTutorials.Contains(tutorial) || ActiveTutorial == tutorial;
    }
}