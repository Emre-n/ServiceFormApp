    $(document).ready(function () {
            var signaturePad = new SignaturePad(document.getElementById('signature-pad'));

    $('#clear').click(function () {
        signaturePad.clear();
            });

    $('#submitForm').click(function (e) {
                if (signaturePad.isEmpty()) {
        alert("Ýmza alaný boþ! Lütfen imza atýn.");
    e.preventDefault();
    return false;
                }

    var data = signaturePad.toDataURL('image/png');
    $('#signatureData').val(data);
            });

    // Kategori > Alt Kategori > Ürün zinciri
    $('#Category').change(function () {
                var categoryId = $(this).val();
    if (categoryId) {
        $.get('@Url.Action("GetSubcategories", "Yonetici")', { categoryId: categoryId }, function (data) {
            var subcategorySelect = $('#Subcategory');
            subcategorySelect.empty().append('<option value="">Alt Kategori Seçiniz</option>');
            $.each(data, function (index, subcategory) {
                subcategorySelect.append('<option value="' + subcategory.Value + '">' + subcategory.Text + '</option>');
            });
            $('.item-select').empty().append('<option value="">Ürün Seçiniz</option>');
        });
                }
            });

    $('#Subcategory').change(function () {
                var subcategoryId = $(this).val();
    if (subcategoryId) {
        $.get('@Url.Action("GetItems", "Yonetici")', { subcategoryId: subcategoryId }, function (data) {
            $('.item-select').each(function () {
                var select = $(this);
                select.empty().append('<option value="">Ürün Seçiniz</option>');
                $.each(data, function (index, item) {
                    select.append('<option value="' + item.Value + '">' + item.Text + '</option>');
                });
            });
        });
                }
            });

    $('#add-item').click(function () {
        let count = $('#item-container .item-dropdown').length;
    if (count < 7) {
        let newItem = $('#item-container .item-dropdown:first').clone();
    newItem.find('select').val('');
    $('#item-container').append(newItem);
                }
            });

    $('#item-container').on('click', '.remove-item', function () {
                if ($('#item-container .item-dropdown').length > 1) {
        $(this).closest('.item-dropdown').remove();
                }
            });
        });