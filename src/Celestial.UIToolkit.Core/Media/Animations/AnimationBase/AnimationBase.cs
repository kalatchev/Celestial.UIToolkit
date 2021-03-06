﻿using System;
using System.Windows.Media.Animation;

namespace Celestial.UIToolkit.Media.Animations
{

    /// <summary>
    ///     Defines a base class for a custom <see cref="AnimationTimeline"/>,
    ///     following the pattern in the .NET Framework (see remarks).
    /// </summary>
    /// <typeparam name="T">
    ///     The type which is being animated by the animation.
    /// </typeparam>
    /// <remarks>
    ///     For more information about the intended design of animation classes,
    ///     see the following information:
    ///     https://docs.microsoft.com/de-de/dotnet/framework/wpf/graphics-multimedia/custom-animations-overview#derive-from-animationtimeline
    /// </remarks>
    public abstract class AnimationBase<T> : AnimationTimeline
    {

        /// <summary>
        /// Gets the target type of the animation..
        /// </summary>
        public override Type TargetPropertyType
        {
            get
            {
                ReadPreamble();
                return typeof(T);
            }
        }

        /// <summary>
        ///     Returns the current value of the animation.
        /// </summary>
        /// <param name="defaultOriginValue">
        ///     The origin value provided to the animation if the animation does not have its
        ///     own start value. If this animation is the first in a composition chain it will
        ///     be the base value of the property being animated; otherwise it will be the value
        ///     returned by the previous animation in the chain.
        /// </param>
        /// <param name="defaultDestinationValue">
        ///     The destination value provided to the animation if the animation does not have
        ///     its own destination value.
        /// </param>
        /// <param name="animationClock">
        ///     A non-specific <see cref="AnimationClock"/> which can generate the <see cref="Clock.CurrentTime"/>
        ///     or <see cref="Clock.CurrentProgress"/> value to be used by the
        ///     animation to generate its output value.
        /// </param>
        /// <returns>The value this animation believes should be the current value for the property.</returns>
        public override sealed object GetCurrentValue(
            object defaultOriginValue, object defaultDestinationValue, AnimationClock animationClock)
        {
            if (animationClock is null)
                throw new ArgumentNullException(nameof(animationClock)); 

            // The custom animations are using an IAnimationClock instead of the WPF class.
            // Use an adapter to proxy the WPF type to the interface.
            var clockAdapter = new AnimationClockAdapter(animationClock);
            return GetCurrentValue(
                defaultOriginValue,
                defaultDestinationValue,
                clockAdapter
            );
        }

        /// <summary>
        ///     Returns the current value of the animation.
        /// </summary>
        /// <param name="defaultOriginValue">
        ///     The origin value provided to the animation if the animation does not have its
        ///     own start value. If this animation is the first in a composition chain it will
        ///     be the base value of the property being animated; otherwise it will be the value
        ///     returned by the previous animation in the chain.
        /// </param>
        /// <param name="defaultDestinationValue">
        ///     The destination value provided to the animation if the animation does not have
        ///     its own destination value.
        /// </param>
        /// <param name="animationClock">
        ///     A non-specific <see cref="IAnimationClock"/> which can generate the <see cref="IClock.CurrentTime"/>
        ///     or <see cref="IClock.CurrentProgress"/> value to be used by the
        ///     animation to generate its output value.
        /// </param>
        /// <returns>The value this animation believes should be the current value for the property.</returns>
        public virtual T GetCurrentValue(
            object defaultOriginValue, object defaultDestinationValue, IAnimationClock animationClock)
        {
            ReadPreamble();
            if (animationClock == null) throw new ArgumentNullException(nameof(animationClock));

            if (!(defaultOriginValue is T) && !(defaultOriginValue is null))
                ThrowForInvalidAnimationValueType(nameof(defaultOriginValue));
            if (!(defaultDestinationValue is T) && !(defaultDestinationValue is null))
                ThrowForInvalidAnimationValueType(nameof(defaultDestinationValue));

            return GetCurrentValueCore(
                (T)defaultOriginValue, (T)defaultDestinationValue, animationClock);
        }

        /// <summary>
        ///     Calculates a value that represents the current value of the property being animated, 
        ///     as determined by the host animation.
        /// </summary>
        /// <param name="defaultOriginValue">
        ///      The suggested origin value, used if the animation 
        ///      does not have its own explicitly set start value.
        /// </param>
        /// <param name="defaultDestinationValue">
        ///     The suggested destination value, used if the animation 
        ///     does not have its own explicitly set end value.
        /// </param>
        /// <param name="animationClock">
        ///     An <see cref="IAnimationClock"/> which can generate the <see cref="IClock.CurrentTime"/>
        ///     or <see cref="IClock.CurrentProgress"/> value to be used by the
        ///     animation to generate its output value.
        /// </param>
        /// <returns>The value this animation believes should be the current value for the property.</returns>
        protected abstract T GetCurrentValueCore(
            T defaultOriginValue,
            T defaultDestinationValue,
            IAnimationClock animationClock);
        
        private void ThrowForInvalidAnimationValueType(string paramName)
        {
            throw new InvalidOperationException(
                $"The animation does not support the type of the provided {paramName}. " +
                $"It expected a parameter of type {typeof(T).FullName}.");
        }
        
    }
    
}
