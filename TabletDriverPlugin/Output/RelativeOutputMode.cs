﻿using System;
using System.Collections.Generic;
using System.Linq;
using TabletDriverPlugin.Attributes;
using TabletDriverPlugin.Platform.Pointer;
using TabletDriverPlugin.Tablet;

namespace TabletDriverPlugin.Output
{
    [PluginIgnore]
    public abstract class RelativeOutputMode : BindingHandler, IOutputMode
    {
        private IEnumerable<IFilter> _filters, _preFilters, _postFilters;
        public IEnumerable<IFilter> Filters
        {
            set
            {
                _filters = value;
                _preFilters = value.Where(f => f.FilterStage == FilterStage.PreTranspose);
                _postFilters = value.Where(f => f.FilterStage == FilterStage.PostTranspose);
            }
            get => _filters;
        }

        public abstract IPointerHandler PointerHandler { get; }

        public float XSensitivity { set; get; }
        public float YSensitivity { set; get; }
        public TimeSpan ResetTime { set; get; }

        private ITabletReport _lastReport;
        private DateTime _lastReceived;
        private Point _lastPosition;

        public virtual void Read(IDeviceReport report)
        {
            if (report is ITabletReport tabletReport)
            {
                if (TabletProperties.ActiveReportID != 0 && tabletReport.ReportID > TabletProperties.ActiveReportID)
                {
                    if (Transpose(tabletReport) is Point pos)
                    {
                        if (PointerHandler is IPressureHandler pressureHandler)
                            pressureHandler.SetPressure((float)tabletReport.Pressure / (float)TabletProperties.MaxPressure);
                        
                        PointerHandler.SetPosition(pos);
                    }
                }
            }
            HandleBinding(report);
        }
        
        internal Point Transpose(ITabletReport report)
        {
            var difference = DateTime.Now - _lastReceived;
            if (difference > ResetTime && _lastReceived != default)
            {
                _lastReport = null;
                _lastPosition = null;
            }

            if (_lastReport != null)
            {
                var pos = new Point(report.Position.X - _lastReport?.Position.X ?? 0, report.Position.Y - _lastReport?.Position.Y ?? 0);

                // Pre Filter
                foreach (IFilter filter in _preFilters)
                    pos = filter.Filter(pos);
                
                // Normalize (ratio of 1)
                pos.X /= TabletProperties.MaxX;
                pos.Y /= TabletProperties.MaxY;

                // Scale to tablet dimensions (mm)
                pos.X *= TabletProperties.Width;
                pos.Y *= TabletProperties.Height;

                // Sensitivity setting
                pos.X *= XSensitivity;
                pos.Y *= YSensitivity;
                
                // Translate by cursor position
                pos += _lastPosition ??= PointerHandler.GetPosition() ?? new Point(0, 0);

                // Post Filter
                foreach (IFilter filter in _postFilters)
                    pos = filter.Filter(pos);

                return pos;
            }
            
            _lastReport = report;
            _lastReceived = DateTime.Now;
            
            return null;
        }
    }
}