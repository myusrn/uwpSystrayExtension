﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace SystrayExtension
{
    public class Margin
    {
        public static readonly DependencyProperty LeftProperty = DependencyProperty.RegisterAttached(
            "Left",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0));

        public static void SetLeft(UIElement element, double value)
        {
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;

                frameworkElement.Margin = new Thickness(value, currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
            }
        }

        public static double GetLeft(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty TopProperty = DependencyProperty.RegisterAttached(
            "Top",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0));

        public static void SetTop(UIElement element, double value)
        {
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;

                frameworkElement.Margin = new Thickness(currentMargin.Left, value, currentMargin.Right, currentMargin.Bottom);
            }
        }

        public static double GetTop(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty RightProperty = DependencyProperty.RegisterAttached(
            "Right",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0));

        public static void SetRight(UIElement element, double value)
        {
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;

                frameworkElement.Margin = new Thickness(currentMargin.Left, currentMargin.Top, value, currentMargin.Bottom);
            }
        }

        public static double GetRight(UIElement element)
        {
            return 0;
        }

        public static readonly DependencyProperty BottomProperty = DependencyProperty.RegisterAttached(
            "Bottom",
            typeof(double),
            typeof(Margin),
            new PropertyMetadata(0.0));

        public static void SetBottom(UIElement element, double value)
        {
            var frameworkElement = element as FrameworkElement;
            if (frameworkElement != null)
            {
                Thickness currentMargin = frameworkElement.Margin;

                frameworkElement.Margin = new Thickness(currentMargin.Left, currentMargin.Top, currentMargin.Right, value);
            }
        }

        public static double GetBottom(UIElement element)
        {
            return 0;
        }
    }
}
