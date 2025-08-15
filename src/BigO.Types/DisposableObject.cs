using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace BigO.Types;

/// <summary>
///     A robust base class that implements <see cref="IDisposable" /> and <see cref="IAsyncDisposable" />,
///     ensuring that cleanup logic runs exactly once and that unmanaged resources are released even
///     if managed cleanup throws.
/// </summary>
/// <remarks>
///     <para>
///         This type is safe to use concurrently from multiple threads for disposal: the first caller that
///         begins disposal wins; subsequent calls are no-ops. During disposal (including the asynchronous
///         path), public members should consider the object unusable and are expected to call
///         <see cref="ThrowIfDisposed" /> at their start.
///     </para>
///     <para>
///         Design highlights:
///     </para>
///     <list type="bullet">
///         <item>
///             <description>
///                 Tri-state lifecycle (<c>Alive</c>, <c>Disposing</c>, <c>Disposed</c>) to distinguish in-flight
///                 disposal.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Non-virtual public orchestrators (<see cref="DisposableObject.Dispose()" />,
///                 <see cref="DisposableObject.DisposeAsync()" />) to
///                 avoid inheritance footguns.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="DisposeUnmanagedResources" /> is always called in a <c>finally</c> to prevent leaks
///                 if managed cleanup throws.
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="DisposeAsyncCore" /> by default calls <see cref="DisposeManagedResources" /> so
///                 <c>await using</c> doesn't skip sync-managed disposals.
///             </description>
///         </item>
///         <item>
///             <description>Finalizers (if a derived type truly needs one) should call <c>base.Dispose(false)</c>.</description>
///         </item>
///     </list>
///     <para>
///         Prefer wrapping native handles in <see cref="SafeHandle" /> and releasing them in
///         <see cref="DisposeUnmanagedResources" />. Avoid introducing finalizers when possible.
///     </para>
/// </remarks>
/// <example>
///     <para>
///         Typical synchronous usage by consumers:
///     </para>
///     <code language="csharp"><![CDATA[
/// using var comp = new MyComponent();
/// comp.DoWork();
/// ]]></code>
///     <para>
///         Typical asynchronous usage by consumers:
///     </para>
///     <code language="csharp"><![CDATA[
/// await using var comp = new MyComponent();
/// await comp.DoWorkAsync();
/// ]]></code>
///     <para>
///         Implementing a derived type that manages both sync- and async-disposable resources,
///         plus an unmanaged handle wrapped in <see cref="SafeHandle" />:
///     </para>
///     <code language="csharp"><![CDATA[
/// using System;
/// using System.IO;
/// using System.Runtime.InteropServices;
/// using System.Threading.Tasks;
/// using Microsoft.Win32.SafeHandles;
/// 
/// sealed class MyComponent : DisposableObject
/// {
///     private FileStream? _stream;              // IAsyncDisposable
///     private SafeFileHandle? _handle;          // SafeHandle (unmanaged wrapper)
///     private System.Threading.CancellationTokenSource? _cts; // IDisposable
/// 
///     public MyComponent()
///     {
///         _stream = new FileStream(Path.GetTempFileName(), FileMode.Open, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.Asynchronous);
///         _cts = new System.Threading.CancellationTokenSource();
///         // Example unmanaged allocation via SafeFileHandle (for illustration only):
///         _handle = new SafeFileHandle(IntPtr.Zero, ownsHandle: true);
///     }
/// 
///     public void DoWork()
///     {
///         ThrowIfDisposed();
///         // ...
///     }
/// 
///     public async Task DoWorkAsync()
///     {
///         ThrowIfDisposed();
///         // ...
///         await Task.CompletedTask;
///     }
/// 
///     // Synchronous managed cleanup for IDisposable-only resources.
///     protected override void DisposeManagedResources()
///     {
///         _cts?.Dispose();
///         _cts = null;
/// 
///         // If consumers used 'using' instead of 'await using', we must also dispose the stream here.
///         _stream?.Dispose();
///         _stream = null;
///     }
/// 
///     // Asynchronous managed cleanup for IAsyncDisposable resources.
///     protected override async ValueTask DisposeAsyncCore()
///     {
///         // Prefer async disposal for async-capable resources when 'await using' is used.
///         if (_stream is not null)
///         {
///             await _stream.DisposeAsync().ConfigureAwait(false);
///             _stream = null;
///         }
/// 
///         // Also release non-async managed resources here so 'await using' cleans them too.
///         _cts?.Dispose();
///         _cts = null;
///     }
/// 
///     // Unmanaged cleanup (or SafeHandle release) — runs in a finally block.
///     protected override void DisposeUnmanagedResources()
///     {
///         _handle?.Dispose(); // SafeHandle.Dispose() is safe to call multiple times.
///         _handle = null;
///     }
/// }
/// ]]></code>
/// </example>
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification =
    "Public orchestrators are intentionally non-virtual. Override points are provided for managed/unmanaged cleanup and async core.")]
// ReSharper disable once InconsistentNaming
public abstract class DisposableObject : IDisposable, IAsyncDisposable
{
    // Lifecycle states
    private const int Alive = 0;
    private const int Disposing = 1;
    private const int Disposed = 2;

    // 0 = alive, 1 = disposing, 2 = disposed
    private int _state;

    /// <summary>
    ///     Gets a value indicating whether this instance has fully completed disposal.
    /// </summary>
    /// <remarks>
    ///     Returns <see langword="true" /> only after the object has transitioned to the <c>Disposed</c> state.
    ///     While disposal is in progress (<c>Disposing</c>), <see cref="ThrowIfDisposed" /> will still consider
    ///     the object unusable.
    /// </remarks>
    public bool IsDisposed => Volatile.Read(ref _state) == Disposed;

    /// <summary>
    ///     Gets a value indicating whether disposal has started (includes both <c>Disposing</c> and <c>Disposed</c> states).
    /// </summary>
    protected bool IsDisposeStarted => Volatile.Read(ref _state) != Alive;

    /// <summary>
    ///     Asynchronously disposes this instance.
    ///     Safe to call multiple times; only the first call performs cleanup.
    /// </summary>
    /// <returns>A <see cref="ValueTask" /> that completes when asynchronous disposal finishes.</returns>
    /// <remarks>
    ///     Invokes <see cref="DisposeAsyncCore" /> to release async-capable managed resources. Unmanaged cleanup
    ///     (<see cref="DisposeUnmanagedResources" />) is performed in a <c>finally</c> block regardless of exceptions
    ///     during managed cleanup. Suppresses finalization afterward.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        if (!BeginDispose())
        {
            // Already disposing or disposed; still suppress to be safe in case a finalizer exists on a derived type.
            GC.SuppressFinalize(this);
            return;
        }

        try
        {
            await DisposeAsyncCore().ConfigureAwait(false);
        }
        finally
        {
            // Ensure native resources are released even if managed cleanup faults.
            DisposeUnmanagedResources();
            EndDispose();
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    ///     Disposes this instance, releasing managed and unmanaged resources.
    ///     Safe to call multiple times; only the first call performs cleanup.
    /// </summary>
    /// <remarks>
    ///     Calls <see cref="DisposeManagedResources" /> (for synchronous managed cleanup) and always calls
    ///     <see cref="DisposeUnmanagedResources" /> in a <c>finally</c> block to ensure native resources are released
    ///     even if managed cleanup throws. Suppresses finalization afterward.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Non-virtual orchestrator used by <see cref="Dispose()" /> and finalizers in derived types.
    ///     Derived types that implement a finalizer should call <c>base.Dispose(false)</c>.
    /// </summary>
    /// <param name="disposing">
    ///     <see langword="true" /> when called from <see cref="Dispose()" />; <see langword="false" /> when called from a
    ///     finalizer.
    /// </param>
    /// <remarks>
    ///     When <paramref name="disposing" /> is <see langword="true" />, <see cref="DisposeManagedResources" /> is invoked.
    ///     In all cases, <see cref="DisposeUnmanagedResources" /> is invoked in a <c>finally</c> block.
    /// </remarks>
    protected void Dispose(bool disposing)
    {
        if (!BeginDispose())
        {
            return;
        }

        try
        {
            if (disposing)
            {
                DisposeManagedResources();
            }
        }
        finally
        {
            DisposeUnmanagedResources();
            EndDispose();
        }
    }

    /// <summary>
    ///     Releases synchronous managed resources. Override to dispose <see cref="IDisposable" /> fields.
    /// </summary>
    /// <remarks>
    ///     This method is invoked when the consumer uses <c>using</c> or calls <see cref="Dispose()" />.
    ///     It is also invoked by the default implementation of <see cref="DisposeAsyncCore" /> so that
    ///     <c>await using</c> callers do not accidentally skip synchronous disposals.
    ///     Implementations must be idempotent and tolerate multiple calls.
    /// </remarks>
    protected virtual void DisposeManagedResources()
    {
        // Derived classes should dispose IDisposable-managed resources here.
    }

    /// <summary>
    ///     Releases asynchronous managed resources. Override to dispose <see cref="IAsyncDisposable" /> fields.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous cleanup work.
    /// </returns>
    /// <remarks>
    ///     The default implementation calls <see cref="DisposeManagedResources" /> so that <c>await using</c> performs
    ///     at least the synchronous managed cleanup. Override this method to asynchronously dispose resources that
    ///     support <see cref="IAsyncDisposable" />. Implementations should also clean up any remaining synchronous
    ///     managed resources to keep both disposal paths equivalent for callers.
    /// </remarks>
    protected virtual ValueTask DisposeAsyncCore()
    {
        DisposeManagedResources();
        return default;
    }

    /// <summary>
    ///     Releases unmanaged resources (or disposes <see cref="SafeHandle" /> wrappers).
    /// </summary>
    /// <remarks>
    ///     This method is called from both synchronous and asynchronous disposal paths in a <c>finally</c> block
    ///     to guarantee execution even if managed cleanup throws. Implementations must be idempotent and tolerate
    ///     multiple calls. Prefer using <see cref="SafeHandle" /> rather than implementing a finalizer.
    /// </remarks>
    protected virtual void DisposeUnmanagedResources()
    {
        // Derived classes should release native resources or SafeHandles here.
    }

    /// <summary>
    ///     Throws <see cref="ObjectDisposedException" /> if the instance is disposing or already disposed.
    ///     Call this at the start of all public members that require a live object.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The instance is not usable because disposal has started or completed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void ThrowIfDisposed()
    {
        if (Volatile.Read(ref _state) != Alive)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool BeginDispose() =>
        Interlocked.CompareExchange(ref _state, Disposing, Alive) == Alive;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EndDispose() =>
        Volatile.Write(ref _state, Disposed);

    // Guidance for derived types that truly need a finalizer:
    // ~YourDerivedType() { base.Dispose(false); }
}