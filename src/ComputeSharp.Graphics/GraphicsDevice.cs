﻿using System.Runtime.CompilerServices;
using ComputeSharp.Core.Extensions;
using ComputeSharp.Core.Helpers;
using ComputeSharp.Core.Interop;
using ComputeSharp.Graphics.Commands;
using ComputeSharp.Graphics.Extensions;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_COMMAND_LIST_TYPE;
using static TerraFX.Interop.D3D12_FEATURE;

namespace ComputeSharp.Graphics
{
    /// <summary>
    /// A <see langword="class"/> that represents a DX12.1-compatible GPU device that can be used to run compute shaders.
    /// </summary>
    public sealed unsafe class GraphicsDevice : NativeObject
    {
        /// <summary>
        /// The underlying <see cref="ID3D12Device"/> wrapped by the current instance.
        /// </summary>
        private ComPtr<ID3D12Device> d3D12Device;

        /// <summary>
        /// The <see cref="ID3D12CommandQueue"/> instance to use for compute operations.
        /// </summary>
        private ComPtr<ID3D12CommandQueue> d3D12ComputeCommandQueue;

        /// <summary>
        /// The <see cref="ID3D12CommandQueue"/> instance to use for copy operations.
        /// </summary>
        private ComPtr<ID3D12CommandQueue> d3D12CopyCommandQueue;

        /// <summary>
        /// The <see cref="ID3D12Fence"/> instance used for compute operations.
        /// </summary>
        private ComPtr<ID3D12Fence> d3D12ComputeFence;

        /// <summary>
        /// The <see cref="ID3D12Fence"/> instance used for copy operations.
        /// </summary>
        private ComPtr<ID3D12Fence> d3D12CopyFence;

        /// <summary>
        /// The <see cref="ID3D12CommandAllocatorPool"/> instance for compute operations.
        /// </summary>
        private readonly ID3D12CommandAllocatorPool computeCommandAllocatorPool;

        /// <summary>
        /// Gets the <see cref="ID3D12CommandAllocatorPool"/> instance for copy operations.
        /// </summary>
        private readonly ID3D12CommandAllocatorPool copyCommandAllocatorPool;

        /// <summary>
        /// The <see cref="ID3D12DescriptorHandleAllocator"/> instance to use when allocating new buffers.
        /// </summary>
        private ID3D12DescriptorHandleAllocator shaderResourceViewDescriptorAllocator;

        /// <summary>
        /// The next fence value for compute operations using <see cref="d3D12ComputeCommandQueue"/>.
        /// </summary>
        private ulong nextD3D12ComputeFenceValue = 1;

        /// <summary>
        /// The next fence value for copy operations using <see cref="d3D12CopyCommandQueue"/>.
        /// </summary>
        private ulong nextD3D12CopyFenceValue = 1;

        /// <summary>
        /// Creates a new <see cref="GraphicsDevice"/> instance for the input <see cref="ID3D12Device"/>.
        /// </summary>
        /// <param name="d3d12device">The <see cref="ID3D12Device"/> to use for the new <see cref="GraphicsDevice"/> instance.</param>
        /// <param name="dxgiDescription1">The available info for the new <see cref="GraphicsDevice"/> instance.</param>
        internal GraphicsDevice(ComPtr<ID3D12Device> d3d12device, DXGI_ADAPTER_DESC1* dxgiDescription1)
        {
            this.d3D12Device = d3d12device;
            this.d3D12ComputeCommandQueue = d3d12device.Get()->CreateCommandQueue(D3D12_COMMAND_LIST_TYPE_COMPUTE);
            this.d3D12CopyCommandQueue = d3d12device.Get()->CreateCommandQueue(D3D12_COMMAND_LIST_TYPE_COPY);
            this.d3D12ComputeFence = d3d12device.Get()->CreateFence();
            this.d3D12CopyFence = d3d12device.Get()->CreateFence();

            this.computeCommandAllocatorPool = new ID3D12CommandAllocatorPool(D3D12_COMMAND_LIST_TYPE_COMPUTE);
            this.copyCommandAllocatorPool = new ID3D12CommandAllocatorPool(D3D12_COMMAND_LIST_TYPE_COPY);
            this.shaderResourceViewDescriptorAllocator = new ID3D12DescriptorHandleAllocator(d3d12device);

            Luid = *(Luid*)&dxgiDescription1->AdapterLuid;
            Name = new string((char*)dxgiDescription1->Description);
            MemorySize = dxgiDescription1->DedicatedVideoMemory;

            var d3D12Options1Data = d3d12device.Get()->CheckFeatureSupport<D3D12_FEATURE_DATA_D3D12_OPTIONS1>(D3D12_FEATURE_D3D12_OPTIONS1);

            ComputeUnits = d3D12Options1Data.TotalLaneCount;
            WavefrontSize = d3D12Options1Data.WaveLaneCountMin;
        }

        /// <summary>
        /// Gets the locally unique identifier for the current device.
        /// </summary>
        public Luid Luid { get; }

        /// <summary>
        /// Gets the name of the current <see cref="GraphicsDevice"/> instance.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the size of the dedicated video memory for the current <see cref="GraphicsDevice"/> instance.
        /// </summary>
        public nuint MemorySize { get; }

        /// <summary>
        /// Gets the number of total lanes on the current device (eg. CUDA cores on an nVidia GPU).
        /// </summary>
        public uint ComputeUnits { get; }

        /// <summary>
        /// Gets the number of lanes in a SIMD wave on the current device (also known as "wavefront size" or "warp width").
        /// </summary>
        public uint WavefrontSize { get; }

        /// <summary>
        /// Gets the underlying <see cref="ID3D12Device"/> wrapped by the current instance.
        /// </summary>
        internal ID3D12Device* D3D12Device => this.d3D12Device;

        /// <inheritdoc cref="ID3D12DescriptorHandleAllocator.Allocate"/>
        internal void AllocateShaderResourceViewDescriptorHandles(
            out D3D12_CPU_DESCRIPTOR_HANDLE d3d12CpuDescriptorHandle,
            out D3D12_GPU_DESCRIPTOR_HANDLE d3d12GpuDescriptorHandle)
        {
            this.shaderResourceViewDescriptorAllocator.Allocate(out d3d12CpuDescriptorHandle, out d3d12GpuDescriptorHandle);
        }

        /// <inheritdoc cref="ID3D12CommandAllocatorPool.GetCommandAllocator"/>
        /// <param name="d3d12CommandListType">The type of command allocator to rent.</param>
        internal ComPtr<ID3D12CommandAllocator> GetCommandAllocator(D3D12_COMMAND_LIST_TYPE d3d12CommandListType)
        {
            return d3d12CommandListType switch
            {
                D3D12_COMMAND_LIST_TYPE_COMPUTE => this.computeCommandAllocatorPool.GetCommandAllocator(this.d3D12Device, this.d3D12ComputeFence),
                D3D12_COMMAND_LIST_TYPE_COPY => this.copyCommandAllocatorPool.GetCommandAllocator(this.d3D12Device, this.d3D12CopyFence),
                _ => ThrowHelper.ThrowArgumentException<ComPtr<ID3D12CommandAllocator>>()
            };
        }

        /// <summary>
        /// Sets the descriptor heap for a given <see cref="ID3D12GraphicsCommandList"/> instance.
        /// </summary>
        /// <param name="d3D12GraphicsCommandList">The input <see cref="ID3D12GraphicsCommandList"/> instance to use.</param>
        internal void SetDescriptorHeapForCommandList(ID3D12GraphicsCommandList* d3D12GraphicsCommandList)
        {
            ID3D12DescriptorHeap* d3D12DescriptorHeap = this.shaderResourceViewDescriptorAllocator.D3D12DescriptorHeap;

            d3D12GraphicsCommandList->SetDescriptorHeaps(1, &d3D12DescriptorHeap);
        }

        /// <summary>
        /// Executes a given command list and waits for the operation to be completed.
        /// </summary>
        /// <param name="commandList">The input <see cref="CommandList"/> to execute.</param>
        internal void ExecuteCommandList(ref CommandList commandList)
        {
            ref readonly ID3D12CommandAllocatorPool commandAllocatorPool = ref Unsafe.NullRef<ID3D12CommandAllocatorPool>();
            ID3D12CommandQueue* d3D12CommandQueue;
            ID3D12Fence* d3D12Fence;
            ulong d3D12FenceValue;

            // Get the target command queue, fence and pool for the list type
            switch (commandList.D3d12CommandListType)
            {
                case D3D12_COMMAND_LIST_TYPE_COMPUTE:
                    commandAllocatorPool = ref this.computeCommandAllocatorPool;
                    d3D12CommandQueue = this.d3D12ComputeCommandQueue;
                    d3D12Fence = this.d3D12ComputeFence;
                    d3D12FenceValue = this.nextD3D12ComputeFenceValue++;
                    break;
                case D3D12_COMMAND_LIST_TYPE_COPY:
                    commandAllocatorPool = ref this.copyCommandAllocatorPool;
                    d3D12CommandQueue = this.d3D12CopyCommandQueue;
                    d3D12Fence = this.d3D12CopyFence;
                    d3D12FenceValue = this.nextD3D12CopyFenceValue++;
                    break;
                default: ThrowHelper.ThrowArgumentException(); return;
            }

            // Execute the command list and signal to the target fence
            d3D12CommandQueue->ExecuteCommandLists(1, commandList.GetD3D12CommandListAddressOf());

            d3D12CommandQueue->Signal(d3D12Fence, d3D12FenceValue).Assert();

            // If the fence value hasn't been reached, wait until the operation completes
            if (d3D12FenceValue > d3D12Fence->GetCompletedValue())
            {
                d3D12Fence->SetEventOnCompletion(d3D12FenceValue, default).Assert();
            }

            // Enqueue the command allocator pool so that it can be reused later
            commandAllocatorPool.Enqueue(commandList.DetachD3D12CommandAllocator());
        }

        /// <inheritdoc/>
        protected override void OnDispose()
        {
            this.d3D12Device.Dispose();
            this.d3D12ComputeCommandQueue.Dispose();
            this.d3D12CopyCommandQueue.Dispose();
            this.d3D12ComputeFence.Dispose();
            this.d3D12CopyFence.Dispose();
            this.computeCommandAllocatorPool.Dispose();
            this.copyCommandAllocatorPool.Dispose();
            this.shaderResourceViewDescriptorAllocator.Dispose();
        }
    }
}
