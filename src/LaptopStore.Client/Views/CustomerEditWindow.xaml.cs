using LaptopStore.Application.DTOs.Customers;
using System.Windows;

namespace LaptopStore.Client.Views;

public partial class CustomerEditWindow : Window
{
    public CreateCustomerDto? ResultCreateDto { get; private set; }
    public UpdateCustomerDto? ResultUpdateDto { get; private set; }
    public bool IsEditMode => _existingCustomer != null;

    private readonly CustomerDto? _existingCustomer;

    public CustomerEditWindow(CustomerDto? existingCustomer = null)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowCaption.Apply(this);
        _existingCustomer = existingCustomer;

        if (existingCustomer != null)
        {
            TxtTitle.Text = $"Chỉnh Sửa Khách Hàng: {existingCustomer.FullName}";
            Title = $"Chỉnh sửa - {existingCustomer.CustomerCode}";

            TxtFullName.Text = existingCustomer.FullName;
            TxtPhone.Text = existingCustomer.PhoneNumber;
            TxtEmail.Text = existingCustomer.Email ?? string.Empty;
            TxtAddress.Text = existingCustomer.Address ?? string.Empty;
            TxtNote.Text = existingCustomer.Note ?? string.Empty;
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        var fullName = TxtFullName.Text.Trim();
        var phone = TxtPhone.Text.Trim();

        if (string.IsNullOrWhiteSpace(fullName))
        {
            MessageBox.Show("Vui lòng nhập họ và tên khách hàng.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtFullName.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            MessageBox.Show("Vui lòng nhập số điện thoại khách hàng.", "Lỗi nhập liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtPhone.Focus();
            return;
        }

        var email = string.IsNullOrWhiteSpace(TxtEmail.Text) ? null : TxtEmail.Text.Trim();
        var address = string.IsNullOrWhiteSpace(TxtAddress.Text) ? null : TxtAddress.Text.Trim();
        var note = string.IsNullOrWhiteSpace(TxtNote.Text) ? null : TxtNote.Text.Trim();

        if (IsEditMode)
        {
            ResultUpdateDto = new UpdateCustomerDto
            {
                Id = _existingCustomer!.Id,
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                Address = address,
                Note = note
            };
        }
        else
        {
            ResultCreateDto = new CreateCustomerDto
            {
                FullName = fullName,
                PhoneNumber = phone,
                Email = email,
                Address = address,
                Note = note
            };
        }

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
