﻿using System;
using System.Diagnostics;
using System.Windows.Media.Animation;

namespace Celestial.UIToolkit.Media.Animations
{

    /// <summary>
    /// Used by the <see cref="KeyFrameResolver{TKeyFrame}"/> to store the resolve result
    /// for a single key frame.
    /// </summary>
    [DebuggerDisplay("ResolvedKeyTime: {ResolvedKeyTime}, IsResolved: {IsResolved}")]
    [DebuggerStepThrough]
    internal sealed class ResolvedKeyFrame<TKeyFrame> : IKeyFrame, IComparable, IComparable<ResolvedKeyFrame<TKeyFrame>>
        where TKeyFrame : IKeyFrame
    {

        /// <summary>
        /// Gets the original key frame.
        /// </summary>
        public TKeyFrame OriginalKeyFrame { get; }

        KeyTime IKeyFrame.KeyTime
        {
            get => this.OriginalKeyFrame.KeyTime;
            set => this.OriginalKeyFrame.KeyTime = value;
        }

        /// <summary>
        /// Gets or sets the original key frame's <see cref="IKeyFrame.KeyTime"/>
        /// property.
        /// This is simply a shortcut.
        /// </summary>
        public KeyTime OriginalKeyTime
        {
            get => this.OriginalKeyFrame.KeyTime;
            set => this.OriginalKeyFrame.KeyTime = value;
        }

        /// <summary>
        /// Gets the original key frame's <see cref="IKeyFrame.Value"/>
        /// property.
        /// This is simply a shortcut.
        /// </summary>
        public object Value
        {
            get => this.OriginalKeyFrame.Value;
            set => this.OriginalKeyFrame.Value = value;
        }

        /// <summary>
        /// Gets a time which is the resulting time of a resolvement
        /// by the <see cref="KeyFrameResolver{TKeyFrame}"/>.
        /// If this frame has not been resolved, this returns <see cref="TimeSpan.Zero"/>.
        /// </summary>
        public TimeSpan ResolvedKeyTime { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this frame has been resolved.
        /// </summary>
        public bool IsResolved { get; private set; }

        public ResolvedKeyFrame(TKeyFrame originalKeyFrame)
        {
            this.OriginalKeyFrame = originalKeyFrame;
        }

        /// <summary>
        /// Sets the <see cref="ResolvedKeyTime"/> and <see cref="IsResolved"/> properties.
        /// </summary>
        /// <param name="resolvedKeyTime">The frame's resolved time.</param>
        public void Resolve(TimeSpan resolvedKeyTime)
        {
            this.ResolvedKeyTime = resolvedKeyTime;
            this.IsResolved = true;
        }

        public int CompareTo(object obj)
        {
            if (!(obj is ResolvedKeyFrame<TKeyFrame> otherFrame))
                throw new ArgumentException($"{nameof(obj)} must be another {nameof(ResolvedKeyFrame<TKeyFrame>)}.");
            return this.CompareTo(otherFrame);
        }

        public int CompareTo(ResolvedKeyFrame<TKeyFrame> otherFrame)
        {
            return this.ResolvedKeyTime.CompareTo(otherFrame.ResolvedKeyTime);
        }

        public bool IsTimeAfter(TimeSpan time)
        {
            return time > this.ResolvedKeyTime;
        }

        public bool IsTimeBeforeOrInside(TimeSpan time)
        {
            return time <= this.ResolvedKeyTime;
        }

        public double GetProgress(TimeSpan time)
        {
            return time.TotalMilliseconds / this.ResolvedKeyTime.TotalMilliseconds;
        }

    }


}
