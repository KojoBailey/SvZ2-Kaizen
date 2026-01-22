using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroControls : IDisposable
{
    public delegate void OnPlayerControlCallback();

    private const int topMargin = 120;

    private const int bottomMargin = 120;

    private Rect kMoveLeftTouchArea;

    private Rect kMoveRightTouchArea;

    public OnPlayerControlCallback onMoveLeft;

    public OnPlayerControlCallback onMoveRight;

    public OnPlayerControlCallback onDontMove;

    private bool alreadyDisposed;

    private List<int> activeInputs = new List<int>();
    
    private int mCurrentDirection = 0;

    public HeroControls()
    {
        SingletonMonoBehaviour<InputManager>.Instance.InputEventUnhandled += InputEventHandler;
        kMoveLeftTouchArea = new Rect(0f, topMargin, Screen.width / 2, Screen.height - topMargin - bottomMargin);
        kMoveRightTouchArea = new Rect(Screen.width / 2, topMargin, Screen.width, Screen.height - topMargin - bottomMargin);
    }

    private bool IsValidTouch(Vector2 pt)
    {
        return kMoveLeftTouchArea.Contains(pt) || kMoveRightTouchArea.Contains(pt);
    }

    private string GetMoveHeroString(int direction)
    {
        switch (direction)
        {
        case  1: return "Move Hero Right";
        case -1: return "Move Hero Left";
        }
        return "";
    }

    private void RunMovementAction(int direction)
    {
        switch (direction)
        {
        case  1: onMoveRight(); return;
        case -1: onMoveLeft(); return;
        }
        onDontMove();
    }
    
    private void UpdatePCControls()
	{
		// If you're holding left and press right, you'll keep moving left,
        // and vice versa (instead of one taking priority over the other).

        var otherDirection = mCurrentDirection * -1;

        if (mCurrentDirection != 0)
        {
            if (!Input.GetButton(GetMoveHeroString(mCurrentDirection)))
            {
                if (Input.GetButton(GetMoveHeroString(otherDirection)))
                {
                    mCurrentDirection = otherDirection;
                }
                else
                {
                    mCurrentDirection = 0;
                }
            }
        }
        else
        {
            if (Input.GetButton(GetMoveHeroString(1)))
            {
                mCurrentDirection = 1;
            }
            else if (Input.GetButton(GetMoveHeroString(-1)))
            {
                mCurrentDirection = -1;
            }
        }

        RunMovementAction(mCurrentDirection);
	}

    private void UpdateMobileControls()
    {
        HandInfo hand = SingletonMonoBehaviour<InputManager>.Instance.Hand;
        int num = 0;
        while (num < activeInputs.Count)
        {
            int num2 = activeInputs[num];
            if (!hand.fingers[num2].IsFingerDown)
            {
                activeInputs.RemoveAt(num);
                continue;
            }
            if (num == 0)
            {
                Vector2 cursorPosition = hand.fingers[num2].CursorPosition;
                if (onMoveLeft != null && kMoveLeftTouchArea.Contains(cursorPosition))
                {
                    onMoveLeft();
                }
                else if (onMoveRight != null && kMoveRightTouchArea.Contains(cursorPosition))
                {
                    onMoveRight();
                }
            }
            num++;
        }
        if (activeInputs.Count == 0 && onDontMove != null)
        {
            onDontMove();
        }
    }

    public void Update()
    {
        if (!Application.isMobilePlatform)
        {
            UpdatePCControls();
        }
        else
        {
            UpdateMobileControls();
        }
    }

    private void InputEventHandler(InputEvent inputEvent)
    {
        if (inputEvent.EventType == InputEvent.EEventType.OnCursorDown)
        {
            activeInputs.Remove(inputEvent.CursorIndex);
            activeInputs.Add(inputEvent.CursorIndex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool isDisposing)
    {
        if (alreadyDisposed)
        {
            return;
        }
        if (SingletonMonoBehaviour<InputManager>.Exists)
        {
            UnityThreadHelper.CallOnMainThread(() =>
            {
                if (SingletonMonoBehaviour<InputManager>.Exists)
                {
                    SingletonMonoBehaviour<InputManager>.Instance.InputEventUnhandled -= InputEventHandler;
                }
            });
        }
        alreadyDisposed = true;
    }

    ~HeroControls()
    {
        Dispose(false);
    }
}
