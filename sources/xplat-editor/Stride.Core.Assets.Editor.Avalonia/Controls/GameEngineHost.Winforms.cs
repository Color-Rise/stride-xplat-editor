// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_GRAPHICS_API_DIRECT3D || STRIDE_GRAPHICS_API_VULKAN) && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)

using System.Windows.Input;
using System.Windows.Interop;
using Stride.Core.Assets.Editor.Avalonia.Interop;
using Point = System.Windows.Point;

namespace Stride.Core.Assets.Editor.Avalonia.Controls;

partial class GameEngineHost : IWin32Window, IKeyboardInputSink
{
    IKeyboardInputSite IKeyboardInputSink.KeyboardInputSite { get; set; }

    private IntPtr ContextMenuWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case NativeHelper.WM_LBUTTONDOWN:
            case NativeHelper.WM_RBUTTONDOWN:
                // We need to change from the context menu coordinates to the HwndHost coordinates and re-encode lParam
                var position = new Point(-(short)(lParam.ToInt64() & 0xFFFF), -((lParam.ToInt64() & 0xFFFF0000) >> 16));
                var offset = contextMenuPosition - position;
                lParam = new IntPtr((short)offset.X + ((short)offset.Y << 16));
                var threadId = NativeHelper.GetWindowThreadProcessId(Handle, IntPtr.Zero);
                NativeHelper.PostThreadMessage(threadId, msg, wParam, lParam);
                break;
            case NativeHelper.WM_DESTROY:
                lock (contextMenuSources)
                {
                    var source = contextMenuSources.First(x => x.Handle == hwnd);
                    source.RemoveHook(ContextMenuWndProc);
                }
                break;
        }
        return IntPtr.Zero;
    }

    //[CanBeNull]
    //[MethodImpl(MethodImplOptions.AggressiveInlining)]
    //private HwndSource GetHwndSource()
    //{
    //    return (HwndSource)PresentationSource.FromVisual(this);
    //}

    IKeyboardInputSite IKeyboardInputSink.RegisterKeyboardInputSink(IKeyboardInputSink sink)
    {
        throw new NotSupportedException();
    }

    bool IKeyboardInputSink.TranslateAccelerator(ref MSG msg, ModifierKeys modifiers)
    {
        return false;
    }

    bool IKeyboardInputSink.TabInto(TraversalRequest request)
    {
        return false;
    }

    bool IKeyboardInputSink.OnMnemonic(ref MSG msg, ModifierKeys modifiers)
    {
        return false;
    }

    bool IKeyboardInputSink.TranslateChar(ref MSG msg, ModifierKeys modifiers)
    {
        return false;
    }

    bool IKeyboardInputSink.HasFocusWithin()
    {
        var focus = NativeHelper.GetFocus();
        return Handle != IntPtr.Zero && (focus == Handle || NativeHelper.IsChild(Handle, focus));
    }
}
#endif
