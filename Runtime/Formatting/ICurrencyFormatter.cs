namespace fefek5.Currency.Runtime
{
    /// <summary>Turns a balance into text. Picked per currency, or per CurrencyLabel.</summary>
    public interface ICurrencyFormatter
    {
        string Format(int value);
    }
}
