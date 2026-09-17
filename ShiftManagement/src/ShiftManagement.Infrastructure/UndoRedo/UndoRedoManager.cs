namespace ShiftManagement.Infrastructure.UndoRedo;

public interface IUndoableAction
{
    string Description { get; }
    void Undo();
    void Redo();
}

public class UndoRedoManager
{
    private readonly Stack<IUndoableAction> _undoStack = new();
    private readonly Stack<IUndoableAction> _redoStack = new();
    private readonly int _maxHistory;

    public event EventHandler? StateChanged;

    public UndoRedoManager(int maxHistory = 50)
    {
        _maxHistory = maxHistory;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? CurrentUndoDescription => CanUndo ? _undoStack.Peek().Description : null;
    public string? CurrentRedoDescription => CanRedo ? _redoStack.Peek().Description : null;

    public void ExecuteAction(IUndoableAction action)
    {
        action.Redo(); // 実行
        _undoStack.Push(action);
        _redoStack.Clear();

        if (_undoStack.Count > _maxHistory)
        {
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            for (int i = items.Length - 2; i >= 0; i--)
            {
                _undoStack.Push(items[i]);
            }
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (!CanUndo) return;

        var action = _undoStack.Pop();
        action.Undo();
        _redoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (!CanRedo) return;

        var action = _redoStack.Pop();
        action.Redo();
        _undoStack.Push(action);

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
