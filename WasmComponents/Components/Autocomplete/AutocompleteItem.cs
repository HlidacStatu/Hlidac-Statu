namespace WasmComponents.Components.Autocomplete;

public class AutocompleteItem<T>
{
    public AutocompleteItem()
    {
    }
    
    public T? Value { get; set; }
    public string? Text { get; set; }

    public AutocompleteItem(string text)
    {
        Text = text;
    }
    
    public AutocompleteItem(T value)
    {
        Value = value;
    }
}