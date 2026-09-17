using ShiftManagement.Core.Models;

namespace ShiftManagement.Core.Interfaces;

/// <summary>勤務表バリデーターのインターフェース（Solverから独立）</summary>
public interface IShiftValidator
{
    /// <summary>勤務表全体を検証する</summary>
    ValidationResult Validate(
        IReadOnlyList<ShiftAssignment> assignments,
        OptimizationInput input);

    /// <summary>手動変更時のリアルタイム検証</summary>
    ValidationResult ValidateSingleChange(
        IReadOnlyList<ShiftAssignment> assignments,
        ShiftAssignment changedAssignment,
        OptimizationInput input);
}
