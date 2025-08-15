using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace BigO.Types;

/// <summary>
///     Base type for objects that need property change notification with optional validation.
///     Provides a single change pipeline with pre/post hooks and optional equality customization.
/// </summary>
/// <remarks>
///     <para>
///         - <see cref="SetField{T}(ref T, T, string?, IEqualityComparer{T}?)" /> returns <see langword="true" /> only
///         when the
///         backing field is assigned a different value (per <see cref="EqualityComparer{T}.Default" /> or the provided
///         comparer),
///         and only then are hooks/events invoked.
///     </para>
///     <para>
///         - Order is guaranteed: equality check → <see cref="ValidatePropertyValue{T}" /> (may throw) → assignment →
///         post-hook
///         (<see cref="OnPropertyChanged{T}" />) → <see cref="PropertyChanged" /> event.
///     </para>
///     <para>
///         - The <see cref="PropertyChanged" /> event is raised synchronously on the caller's thread; no marshalling is
///         performed.
///     </para>
///     <para>- Thread-safety: none. Callers must synchronize if required. Reentrancy is not prevented.</para>
///     <para>
///         - Floating point: <c>EqualityComparer&lt;double&gt;.Default.Equals(double.NaN, double.NaN)</c> and likewise for
///         <c>float</c> both return <c>true</c>. For tolerance-based comparisons, supply a custom comparer.
///     </para>
///     <para>
///         - Collections: default equality for arrays/most collections is reference equality. Provide a comparer if
///         structural
///         equality is desired.
///     </para>
///     <para>
///         - To indicate “all properties changed”, call <see cref="RaiseAllPropertiesChanged" /> (or pass <c>null</c>/
///         <see cref="string.Empty" /> to
///         <see cref="RaisePropertyChanged(string?)" />).
///     </para>
/// </remarks>
/// <example>
///     <para>Example with custom equality comparer for floating-point tolerance:</para>
///     <code>
/// public double Temperature
/// {
///     get => _temperature;
///     set => SetField(ref _temperature, value, comparer: new DoubleComparer(0.001));
/// }
/// </code>
///     <para>Example with dependent properties:</para>
///     <code>
/// public string FirstName
/// {
///     get => _firstName;
///     set
///     {
///         if (SetField(ref _firstName, value))
///             RaisePropertyChanged(nameof(FullName));
///     }
/// }
/// </code>
/// </example>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <summary>
    ///     Conventional name for indexer notifications in XAML stacks (e.g., WPF).
    ///     Call <see cref="RaisePropertyChanged(string?)" /> with this value to indicate the indexer changed.
    /// </summary>
    protected const string IndexerName = "Item[]";

    private static readonly ConcurrentDictionary<string, PropertyChangedEventArgs> EventArgsCache = new();
    private static readonly PropertyChangedEventArgs AllChangedArgs = new(null);

    /// <summary>Occurs when a property value changes.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    ///     Sets <paramref name="field" /> to <paramref name="value" /> if different. Calls
    ///     <see cref="ValidatePropertyValue{T}" /> before assignment (can throw to reject) and
    ///     <see cref="OnPropertyChanged{T}" /> after assignment, then raises <see cref="PropertyChanged" />.
    /// </summary>
    /// <typeparam name="T">Field type.</typeparam>
    /// <param name="field">Backing field reference.</param>
    /// <param name="value">Proposed value.</param>
    /// <param name="propertyName">Name of the property (supplied by the compiler when omitted).</param>
    /// <param name="comparer">Optional comparer. Defaults to <see cref="EqualityComparer{T}.Default" />.</param>
    /// <returns><see langword="true" /> if the field was updated; otherwise <see langword="false" />.</returns>
    /// <exception cref="ArgumentException">Thrown when <see cref="ValidatePropertyValue{T}" /> rejects the new value.</exception>
    protected bool SetField<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null,
        IEqualityComparer<T>? comparer = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        var oldValue = field;
        comparer ??= EqualityComparer<T>.Default;

        if (comparer.Equals(oldValue, value))
        {
            return false;
        }

        ValidatePropertyValue(propertyName, oldValue, value);

        field = value;

        OnPropertyChanged(propertyName, oldValue, value);
        RaisePropertyChanged(propertyName);

        return true;
    }

    /// <summary>
    ///     Validates a property value before it's set. Override to add validation logic.
    ///     Throw an exception to reject the change. Only invoked when the comparer indicates a change.
    /// </summary>
    protected virtual void ValidatePropertyValue<T>(string propertyName, T oldValue, T newValue)
    {
        // Base implementation does nothing
    }

    /// <summary>
    ///     Post-change hook. Called after the property value has been updated, before the <see cref="PropertyChanged" />
    ///     event.
    /// </summary>
    protected virtual void OnPropertyChanged<T>(string propertyName, T oldValue, T newValue)
    {
        // Base implementation does nothing
    }

    /// <summary>
    ///     Raises the <see cref="PropertyChanged" /> event for the specified property.
    ///     Pass <c>null</c> or <see cref="string.Empty" /> to indicate that all properties have changed.
    /// </summary>
    /// <param name="propertyName">
    ///     The name of the property that changed, or <c>null</c>/<see cref="string.Empty" /> to
    ///     indicate all properties.
    /// </param>
    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        var handler = PropertyChanged;
        if (handler is null)
        {
            return;
        }

        var args = string.IsNullOrEmpty(propertyName)
            ? AllChangedArgs
            : EventArgsCache.GetOrAdd(propertyName, static n => new PropertyChangedEventArgs(n));

        // Invoke each handler separately to prevent one exception from blocking others
        foreach (var @delegate in handler.GetInvocationList())
        {
            var singleHandler = (PropertyChangedEventHandler)@delegate;
            singleHandler(this, args);
        }
    }

    /// <summary>Raises <see cref="PropertyChanged" /> for all properties.</summary>
    protected void RaiseAllPropertiesChanged() => RaisePropertyChanged(null);
}