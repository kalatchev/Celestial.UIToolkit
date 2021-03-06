﻿using Celestial.UIToolkit.Extensions;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;

namespace Celestial.UIToolkit.Controls
{

    /// <summary>
    /// Represents a switch which allows users to turn something on or off.
    /// </summary>
    [ContentProperty(nameof(Header))]
    [TemplateVisualState(Name = ActiveVisualState, GroupName = CommonStatesVisualStateGroup)]
    [TemplateVisualState(Name = MouseOverActiveVisualState, GroupName = CommonStatesVisualStateGroup)]
    [TemplateVisualState(Name = PressedVisualState, GroupName = CommonStatesVisualStateGroup)]
    [TemplateVisualState(Name = OnVisualState, GroupName = ToggledStatesVisualStateGroup)]
    [TemplateVisualState(Name = OffVisualState, GroupName = ToggledStatesVisualStateGroup)]
    [TemplateVisualState(Name = DraggingVisualState, GroupName = ToggledStatesVisualStateGroup)]
    [TemplateVisualState(Name = OnContentVisualState, GroupName = ContentDisplayStatesVisualStateGroup)]
    [TemplateVisualState(Name = OffContentVisualState, GroupName = ContentDisplayStatesVisualStateGroup)]
    [TemplatePart(Name = DraggableAreaTemplatePart, Type = typeof(Thumb))]
    public partial class ToggleSwitch : CommonVisualStatesControl
    {

        internal const string DraggableAreaTemplatePart = "PART_DraggableArea";

        // The following three states are introduced for styling only.
        // The On/Off/Dragging states are meant for moving the knob around.
        // For color manipulation, these "CommonStates" exist.
        // Together with the base class' states, the following mapping exists:
        // - Off           <>  Normal
        // - MouseOverOff  <>  MouseOver
        // - Dragging      <>  Pressed
        // - MouseOverOn   <>  MouseOverActive
        // - On            <>  Active

        /// <summary>
        /// Defines the name of the "Active" Visual State.
        /// </summary>
        public const string ActiveVisualState = "Active";

        /// <summary>
        /// Defines the name of the "MouseOverActive" Visual State.
        /// </summary>
        public const string MouseOverActiveVisualState = "MouseOverActive";

        /// <summary>
        /// Defines the name of the "Pressed" Visual State.
        /// </summary>
        public const string PressedVisualState = "Pressed";

        /// <summary>
        /// Defines the name of the "ToggledStates" Visual State Group.
        /// </summary>
        public const string ToggledStatesVisualStateGroup = "ToggledStates";

        /// <summary>
        /// Defines the name of the "On" Visual State.
        /// </summary>
        public const string OnVisualState = "On";

        /// <summary>
        /// Defines the name of the "Off" Visual State.
        /// </summary>
        public const string OffVisualState = "Off";

        /// <summary>
        /// Defines the name of the "Dragging" Visual State.
        /// </summary>
        public const string DraggingVisualState = "Dragging";

        /// <summary>
        /// Defines the name of the "ContentDisplayStates" Visual State Group.
        /// </summary>
        public const string ContentDisplayStatesVisualStateGroup = "ContentDisplayStates";

        /// <summary>
        /// Defines the name of the "OnContent" Visual State.
        /// </summary>
        public const string OnContentVisualState = "OnContent";

        /// <summary>
        /// Defines the name of the "OffContent" Visual State.
        /// </summary>
        public const string OffContentVisualState = "OffContent";

        private Thumb _draggableArea;
        private bool _isDraggingViaKey;
        private bool _isDraggingViaMouse;
        private bool _isDraggingViaTouch;
        private double _dragStartKnobOffset;
        private bool _wasKnobDragged;
        private Point _touchDownPoint;
        
        /// <summary>
        /// Identifies the <see cref="Toggled"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ToggledEvent =
            EventManager.RegisterRoutedEvent(
                nameof(Toggled),
                RoutingStrategy.Bubble,
                typeof(RoutedEventHandler),
                typeof(ToggleSwitch));

        /// <summary>
        /// Occurs when the <see cref="ToggleSwitch"/> is toggled on or off.
        /// </summary>
        [Category("Behavior")]
        public event RoutedEventHandler Toggled
        {
            add { AddHandler(ToggledEvent, value); }
            remove { RemoveHandler(ToggledEvent, value); }
        }

        static ToggleSwitch()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
            IsManipulationEnabledProperty.OverrideMetadata(
                typeof(ToggleSwitch), new PropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleSwitch"/> class.
        /// </summary>
        public ToggleSwitch()
            : this(false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ToggleSwitch"/> class.
        /// </summary>
        /// <param name="isOn">
        /// A value indicating whether the switch is currently "On".
        /// </param>
        public ToggleSwitch(bool isOn)
        {
            IsOn = isOn;

            // These event handlers are required for dragging/toggling the switch.
            PreviewKeyDown += ToggleSwitch_PreviewKeyDown;
            PreviewKeyUp += ToggleSwitch_PreviewKeyUp;
            PreviewMouseLeftButtonDown += (sender, e) => Keyboard.Focus(this);

            Loaded += (sender, e) =>
            {
                VisualStateManager.GoToState(this, NormalVisualState, false);
                EnterCurrentVisualStates();
            };
        }

        /// <summary>
        /// Invoked when the <see cref="ToggleSwitch"/>'s template is applied.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _draggableArea = GetTemplateChild(DraggableAreaTemplatePart) as Thumb;
            if (_draggableArea != null)
            {
                // Drag events.
                _draggableArea.DragStarted += DraggableArea_DragStarted;
                _draggableArea.DragCompleted += DraggableArea_DragCompleted;
                _draggableArea.DragDelta += DraggableArea_DragDelta;

                // Touch events.
                _draggableArea.PreviewTouchDown += DraggableArea_PreviewTouchDown;
                _draggableArea.PreviewTouchMove += DraggableArea_PreviewTouchMove;
                _draggableArea.PreviewTouchUp += DraggableArea_PreviewTouchUp;
            }
        }

        // The Thumb's Drag Events can directly be translated into our OnKnobDrag-Calls.
        private void DraggableArea_DragStarted(object sender, DragStartedEventArgs e)
        {
            if (!IsDragging)
            {
                this.TraceVerbose("Invoking drag operation via mouse.");
                _isDraggingViaMouse = true;
                OnKnobDragStarted();
            }
        }
        
        private void DraggableArea_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (_isDraggingViaMouse)
            {
                OnKnobDragDelta(new Point(e.HorizontalChange, e.VerticalChange));
            }
        }

        private void DraggableArea_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (_isDraggingViaMouse)
            {
                this.TraceVerbose("Drag via mouse completed.");
                _isDraggingViaMouse = false;
                OnKnobDragCompleted();
            }
        }

        // Touch events are a little harder.
        // WPF doesn't provide a DragDelta event in the TouchMove API, so we have to manually
        // remember the starting point and calculate the delta.
        private void DraggableArea_PreviewTouchDown(object sender, TouchEventArgs e)
        {
            if (!IsDragging)
            {
                this.TraceVerbose("Invoking drag operation via touch.");
                _touchDownPoint = e.GetTouchPoint(this).Position;
                _isDraggingViaTouch = true;
                OnKnobDragStarted();
            }
        }

        private void DraggableArea_PreviewTouchMove(object sender, TouchEventArgs e)
        {
            if (_isDraggingViaTouch)
            {
                Point currentPoint = e.GetTouchPoint(this).Position;
                Point delta = (Point)(currentPoint - _touchDownPoint);
                OnKnobDragDelta(delta);
            }
        }

        private void DraggableArea_PreviewTouchUp(object sender, TouchEventArgs e)
        {
            if (_isDraggingViaTouch)
            {
                this.TraceVerbose("Drag via touch completed.");
                _isDraggingViaTouch = false;
                OnKnobDragCompleted();
            }
        }

        // Dragging via Keys is easy.
        // Simply wait until the user releases the key and then toggle IsOn.
        // There is no DragDelta with key presses.
        private void ToggleSwitch_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsDragging && e.Key == Key.Space)
            {
                this.TraceVerbose("Invoking drag operation via key.");
                _isDraggingViaKey = true;
                OnKnobDragStarted();
            }
        }

        private void ToggleSwitch_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (_isDraggingViaKey && e.Key == Key.Space)
            {
                // The user released the previously pressed Toggle-Key.
                this.TraceVerbose("Drag via key completed.");
                _isDraggingViaKey = false;
                OnKnobDragCompleted();
            }
        }

        /// <summary>
        /// Once called, initiates a dragging sequence, until stopped via 
        /// <see cref="OnKnobDragCompleted"/>.
        /// During this time, drag events relayed via <see cref="OnKnobDragDelta(Point)"/>
        /// are translated to the <see cref="KnobOffset"/> property to move the knob.
        /// </summary>
        private void OnKnobDragStarted()
        {
            if (!IsDragging)
            {
                IsDragging = true;
                _wasKnobDragged = false;
                KnobOffset = (IsOn ? OnKnobOffset : OffKnobOffset) ?? KnobOffset;
                _dragStartKnobOffset = KnobOffset;

                this.TraceVerbose("Dragging invoked.");
                this.TraceVerbose("Drag started at offset {0}.", _dragStartKnobOffset);
                
                EnterCurrentVisualStates();
            }
        }

        /// <summary>
        /// When called, updates the <see cref="KnobOffset"/> depending on the specified
        /// <paramref name="dragDelta"/>.
        /// </summary>
        /// <param name="dragDelta">A point indicating how much the user dragged the knob.</param>
        private void OnKnobDragDelta(Point dragDelta)
        {
            double offsetChange;
            if (DragOrientation == Orientation.Horizontal)
                offsetChange = dragDelta.X;
            else
                offsetChange = dragDelta.Y;

            // As soon as the user drags the knob a little bit, set this to true.
            // This prevents the switch from toggling via a mouse "click".
            if (offsetChange > 0)
                _wasKnobDragged = true;

            // We can simply set the knob offset properties to the DragDelta values,
            // since the CoerceValueCallback will take care of applying a min/max value.
            KnobOffset = _dragStartKnobOffset + offsetChange;
        }

        /// <summary>
        /// Stops a dragging sequence and potentially updates the <see cref="IsOn"/> property,
        /// depending on how the user dragged the switch's knob.
        /// </summary>
        private void OnKnobDragCompleted()
        {
            if (IsDragging)
            {
                this.TraceVerbose("Dragging stopped.");
                IsDragging = false;

                if (!_wasKnobDragged)
                {
                    // If we get here, the user clicked the switch, but didn't drag it.
                    // Simply switch IsOn in this case.
                    this.TraceVerbose("Drag completed. No actual dragging was done. Treating as clicked.");
                    IsOn = !IsOn;
                }
                else
                {
                    // If we get here, the knob was dragged.
                    // If we know about OnKnobOffset and OffKnobOffset, we can calculate the distance
                    // between them.
                    // This allows us to only switch IsOn, if the user dragged the knob more than
                    // half the distance. Otherwise, the switch will stay in its current stay and
                    // the knob will only snap back.
                    if (OnKnobOffset.HasValue && OffKnobOffset.HasValue)
                    {
                        double minRequiredDistance = Math.Abs(OffKnobOffset.Value - OnKnobOffset.Value) / 2;
                        double actualOffsetChange = Math.Abs(_dragStartKnobOffset - KnobOffset);

                        // Only change IsOn, if the user dragged the switch over more than half of 
                        // the switch.
                        if (actualOffsetChange >= minRequiredDistance)
                        {
                            this.TraceVerbose(
                                "Knob was dragged past the minimum required distance. Got {0} and required {1}.",
                                actualOffsetChange, minRequiredDistance
                            );
                            IsOn = !IsOn;
                        }
                        else
                        {
                            // We didn't quite reach the minimum distance, so IsOn stays. 
                            // Manually update the visual states, so that we go from Dragging -> Previous.
                            this.TraceVerbose(
                                "Didn't drag past the minimum distance for changing IsOn. Got {0}, but required {1}.",
                                actualOffsetChange,
                                minRequiredDistance
                            );
                            EnterCurrentVisualStates();
                        }
                    }
                    else
                    {
                        // We don't know enough about OnKnobOffset and OffKnobOffset, 
                        // so simply switch IsOn.
                        this.TraceVerbose(
                            "Knob was dragged, but there is no information about how far. " +
                            "Assuming that it was enough to change IsOn."
                        );
                        IsOn = !IsOn;
                    }
                }
            }
        }

        private double CoerceKnobOffset(double value)
        {
            // In this callback, we can ensure that the KnobOffset falls in the range of
            // the allowed KnobOnOffset and KnobOffOffset values.
            // This will only work though if both properties have a value.
            if (OnKnobOffset.HasValue && OffKnobOffset.HasValue)
            {
                double min = Math.Min(OnKnobOffset.Value, OffKnobOffset.Value);
                double max = Math.Max(OnKnobOffset.Value, OffKnobOffset.Value);
                value = Math.Min(max, Math.Max(value, min));
            }

            return value;
        }

        private void IsOn_Changed(DependencyPropertyChangedEventArgs e)
        {
            this.TraceInfo($"{nameof(IsOn)} changed to {{0}}.", IsOn);
            OnToggled();
            EnterCurrentVisualStates();
            ExecuteOnOffCommands();
        }

        /// <summary>
        /// Called whenever the <see cref="IsOnProperty"/> dependency property changes.
        /// If not overridden, raises the <see cref="Toggled"/> event.
        /// </summary>
        protected virtual void OnToggled()
        {
            var args = new RoutedEventArgs(ToggledEvent, this);
            RaiseEvent(args);
        }

        private void ExecuteOnOffCommands()
        {
            // Make sure to raise the right command, depending on the IsOn state.
            // Also ensure that each command is executable.
            if (IsOn && OnCommand != null && OnCommand.CanExecute(OnCommandParameter))
            {
                this.TraceVerbose("Executing OnCommand.");
                OnCommand.Execute(OnCommandParameter);
            }
            if (!IsOn && OffCommand != null && OffCommand.CanExecute(OffCommandParameter))
            {
                this.TraceVerbose("Executing OffCommand.");
                OffCommand.Execute(OffCommandParameter);
            }
        }
        
        /// <summary>
        /// Changes ALL visual states of the <see cref="ToggleSwitch"/> to the appropriate one.
        /// </summary>
        private void EnterCurrentVisualStates(bool useTransitions = true)
        {
            // Dragging takes priority over On/Off.
            if (IsDragging)
            {
                VisualStateManager.GoToState(this, DraggingVisualState, useTransitions);
            }
            else
            {
                if (IsOn)
                {
                    VisualStateManager.GoToState(this, OnVisualState, useTransitions);
                    VisualStateManager.GoToState(this, OnContentVisualState, useTransitions);
                }
                else
                {
                    VisualStateManager.GoToState(this, OffVisualState, useTransitions);
                    VisualStateManager.GoToState(this, OffContentVisualState, useTransitions);
                }
            }

            // Also update the CommonStates.
            EnterCurrentCommonStatesVisualState(useTransitions);
        }

        /// <summary>
        /// Enters the current common visual state.
        /// </summary>
        /// <param name="useTransitions">
        /// A value indicating whether visual transitions should be used.
        /// </param>
        protected override void EnterCurrentCommonStatesVisualState(bool useTransitions)
        {
            if (!IsEnabled)
            {
                VisualStateManager.GoToState(this, DisabledVisualState, useTransitions);
            }
            else if (IsDragging)
            {
                VisualStateManager.GoToState(this, PressedVisualState, useTransitions);
            }
            else if (IsMouseOver)
            {
                string state = IsOn ? MouseOverActiveVisualState 
                                    : MouseOverVisualState;
                VisualStateManager.GoToState(this, state, useTransitions);
            }
            else
            {
                string state = IsOn ? ActiveVisualState
                                    : NormalVisualState;
                VisualStateManager.GoToState(this, state, useTransitions);
            }
        }

        /// <summary>
        /// Returns a string representation of the <see cref="ToggleSwitch"/>.
        /// </summary>
        /// <returns>A string representing the <see cref="ToggleSwitch"/>.</returns>
        public override string ToString()
        {
            return $"{nameof(ToggleSwitch)}: " +
                   $"{nameof(IsOn)}: {IsOn}";
        }

    }

}
