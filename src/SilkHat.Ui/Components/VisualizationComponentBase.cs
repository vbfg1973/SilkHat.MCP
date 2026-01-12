using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SilkHat.Ui.Components
{
    public abstract class VisualizationComponentBase : ComponentBase, IAsyncDisposable
    {
        private DotNetObjectReference<VisualizationComponentBase>? _dotNetRef;
        private bool _registered;
        protected string ContainerId { get; } = $"visual-{Guid.NewGuid():N}";

        [Inject] protected IJSRuntime JSRuntime { get; set; } = default!;

        protected abstract bool HasContent { get; }

        public virtual async ValueTask DisposeAsync()
        {
            if (_registered)
            {
                await JSRuntime.InvokeVoidAsync("silkhatVisualHost.unregister", ContainerId);
                _registered = false;
            }

            _dotNetRef?.Dispose();
        }

        protected abstract Task RenderAsync(bool force);
        protected abstract Task ClearAsync();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                _dotNetRef = DotNetObjectReference.Create(this);
                await JSRuntime.InvokeVoidAsync("silkhatVisualHost.register", ContainerId, _dotNetRef);
                _registered = true;
            }

            await RequestRenderAsync(false);
        }

        protected async Task RequestRenderAsync(bool force)
        {
            if (!HasContent)
            {
                await ClearAsync();
                return;
            }

            await RenderAsync(force);
        }

        [JSInvokable]
        public Task NotifyRenderRequested()
        {
            return RequestRenderAsync(true);
        }
    }
}