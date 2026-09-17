using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShiftManagement.Core.Interfaces;
using ShiftManagement.Core.Models;

namespace ShiftManagement.App.ViewModels;

public partial class StaffManagementViewModel : ObservableObject
{
    private readonly IStaffRepository _staffRepo;

    [ObservableProperty]
    private ObservableCollection<Staff> _staffList = new();

    [ObservableProperty]
    private Staff? _selectedStaff;

    [ObservableProperty]
    private string _editLastName = "";

    [ObservableProperty]
    private string _editFirstName = "";

    [ObservableProperty]
    private int _editDisplayOrder;

    [ObservableProperty]
    private bool _editIsActive = true;

    [ObservableProperty]
    private string _statusMessage = "";

    public StaffManagementViewModel(IStaffRepository staffRepo)
    {
        _staffRepo = staffRepo;
    }

    [RelayCommand]
    public async Task LoadStaffAsync()
    {
        var list = await _staffRepo.GetAllActiveAsync();
        StaffList = new ObservableCollection<Staff>(list);
        if (StaffList.Count > 0)
        {
            SelectedStaff = StaffList[0];
        }
    }

    partial void OnSelectedStaffChanged(Staff? value)
    {
        if (value != null)
        {
            EditLastName = value.LastName;
            EditFirstName = value.FirstName;
            EditDisplayOrder = value.DisplayOrder;
            EditIsActive = value.IsActive;
        }
    }

    [RelayCommand]
    public async Task SaveStaffAsync()
    {
        if (SelectedStaff == null) return;

        SelectedStaff.LastName = EditLastName.Trim();
        SelectedStaff.FirstName = EditFirstName.Trim();
        SelectedStaff.DisplayName = $"{SelectedStaff.LastName}　{SelectedStaff.FirstName}";
        SelectedStaff.DisplayOrder = EditDisplayOrder;
        SelectedStaff.IsActive = EditIsActive;

        await _staffRepo.UpdateAsync(SelectedStaff);
        StatusMessage = $"{SelectedStaff.DisplayName} の情報を保存しました。";
        await LoadStaffAsync();
    }

    [RelayCommand]
    public async Task AddStaffAsync()
    {
        var newStaff = new Staff
        {
            LastName = "新規",
            FirstName = "職員",
            DisplayName = "新規　職員",
            JobTypeId = 2, // 介護職
            EmploymentTypeId = 1, // 常勤
            DisplayOrder = StaffList.Count + 1,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _staffRepo.AddAsync(newStaff);
        StatusMessage = "新規職員を追加しました。";
        await LoadStaffAsync();
        SelectedStaff = StaffList.FirstOrDefault(s => s.Id == newStaff.Id);
    }

    [RelayCommand]
    public async Task DeactivateStaffAsync()
    {
        if (SelectedStaff == null) return;
        await _staffRepo.DeactivateAsync(SelectedStaff.Id);
        StatusMessage = $"{SelectedStaff.DisplayName} を無効化しました。";
        await LoadStaffAsync();
    }
}
