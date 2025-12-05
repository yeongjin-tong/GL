using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface ICommand
{
    void Execute();
    void Undo();
}

public class CommandManager : MonoBehaviour
{
    public static CommandManager Instance { get; private set; }

    [Header("UI Buttons")]
    public Button undoBtn;
    public Button redoBtn;

    private Stack<ICommand> undoStack = new Stack<ICommand>();
    private Stack<ICommand> redoStack = new Stack<ICommand>();

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        undoBtn.onClick.AddListener(Undo);
        redoBtn.onClick.AddListener(Redo);

        UpdateUI();
    }

    // ✨ [핵심] 새로운 행동을 했을 때 호출 (SymbolController 등에서 호출)
    public void AddCommand(ICommand command)
    {
        // 1. 새 행동을 Undo 스택에 넣음
        undoStack.Push(command);

        // 2. 새로운 행동을 하면 Redo(앞으로 가기) 스택은 초기화됨 (갈래가 바뀌므로)
        redoStack.Clear();

        // 3. UI 업데이트
        UpdateUI();
    }

    public void Undo()
    {
        if (undoStack.Count == 0) return;

        // 1. 가장 최근 행동을 꺼냄
        ICommand cmd = undoStack.Pop();

        // 2. 행동 취소 발생
        cmd.Undo();

        // 3. Redo 스택으로 보냄
        redoStack.Push(cmd);

        UpdateUI();
    }

    public void Redo()
    {
        if (redoStack.Count == 0) return;

        // 1. Redo 스택에서 꺼냄
        ICommand cmd = redoStack.Pop();

        // 2. 행동 다시 실행
        cmd.Execute();

        undoStack.Push(cmd);

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (undoBtn != null) undoBtn.interactable = undoStack.Count > 0;
        if (redoBtn != null) redoBtn.interactable = redoStack.Count > 0;
    }

    public void ClearHistory()
    {
        undoStack.Clear();
        redoStack.Clear();
        UpdateUI();
    }
}
