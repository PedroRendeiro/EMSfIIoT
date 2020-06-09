using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Highsoft.Web.Mvc.Stocks;
using Highsoft.Web.Mvc.Stocks.Rendering;

using EMSfIIoT_API.Models;
using Microsoft.EntityFrameworkCore.Internal;
using System.Linq;
using SharedAPI;
using Microsoft.AspNetCore.Mvc.Localization;

namespace EMSfIIoT.Pages.Gateways
{
    public class ChartsModel : PageModel
    {
        private readonly ILogger<ChartsModel> _logger;
        private readonly IHtmlLocalizer<ChartsModel> _modelLocalizer;

        public Dictionary<string, List<LineSeriesData>> chartData;
        public ChartsModel(ILogger<ChartsModel> logger, IHtmlLocalizer<ChartsModel> modelLocalizer)
        {
            _logger = logger;
            _modelLocalizer = modelLocalizer;
        }

        public void OnGet()
        {
            HttpContext.Request.Cookies.TryGetValue("timezone", out var timezone);
            
            MeasuresApiConnector.GetMeasures(HttpContext, out IEnumerable<Measure> measures);

            foreach (Measure measure in measures)
            {
                string device = "ESP32_CAM" + measure.LocationID.ToString() + ":1.8." + measure.MeasureTypeID.ToString();

                List<LineSeriesData> data = ViewData[device] as List<LineSeriesData>;

                if (data == null)
                    ViewData[device] = new List<LineSeriesData>();


                (ViewData[device] as List<LineSeriesData>).Add(new LineSeriesData
                    {
                        X = measure.TimeStamp
                            .AddMinutes(Convert.ToDouble(timezone))
                            .Subtract(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                            .TotalMilliseconds,
                        Y = measure.Value,
                        Id = measure.Id.ToString()
                    });
            }

            var chartOptions = new Highstock
            {
                ID = "chart",

                Chart = new Chart
                {
                    Type = ChartType.Line,
                    ZoomType = ChartZoomType.X,
                    BackgroundColor = "transparent"
                },

                /*Title = new Title
                {
                    Text = "Snow depth at Vikjafjellet, Norway"
                },*/

                RangeSelector = new RangeSelector
                {
                    Enabled = true,
                    AllButtonsEnabled = true,
                    Selected = 3,
                    Buttons = new List<RangeSelectorButton>
                    {
                        new RangeSelectorButton
                        {
                            Type = "week",
                            Count = 2,
                            Text = "2" + _modelLocalizer["w"].Value
                        },
                        new RangeSelectorButton
                        {
                            Type = "week",
                            Count = 1,
                            Text = "1" + _modelLocalizer["w"].Value
                        },
                        new RangeSelectorButton
                        {
                            Type = "day",
                            Count = 3,
                            Text = "3" + _modelLocalizer["d"].Value
                        },
                        new RangeSelectorButton
                        {
                            Type = "day",
                            Count = 1,
                            Text = "1" + _modelLocalizer["d"].Value
                        },
                        new RangeSelectorButton
                        {
                            Type = "hour",
                            Count = 6,
                            Text = "6" + _modelLocalizer["hr"].Value
                        },
                        new RangeSelectorButton
                        {
                            Type = "hour",
                            Count = 1,
                            Text = "1" + _modelLocalizer["hr"].Value
                        }
                    }
                },

                XAxis = new List<XAxis>
                {
                    new XAxis
                    {
                        Title = new XAxisTitle
                        {
                            Text = _modelLocalizer["Date"].Value
                        },
                        DateTimeLabelFormats = new Hashtable
                        {

                        }
                    }
                },

                YAxis = new List<YAxis>
                {
                    new YAxis
                    {
                        Title = new YAxisTitle
                        {
                            Text = _modelLocalizer["Consumption"].Value + " [kWh]"
                        }
                    }
                },

                Legend = new Legend
                {
                    Enabled = true
                },

                Tooltip = new Tooltip
                {
                    HeaderFormat = "<b>{series.name}</b>",
                    PointFormat = "{point.x:%H:%M %e/%m} <b>{point.y: .0f} kWh<b>"

                },

                PlotOptions = new PlotOptions
                {
                    Series = new PlotOptionsSeries
                    {
                        Marker = new PlotOptionsSeriesMarker
                        {
                            Enabled = true
                        },
                        ShowInLegend = true,
                        TurboThreshold = 5000,
                        Selected = true,
                        StickyTracking = false,
                        Visible = true
                    }
                },

                Series = new List<Series>(),

                Credits = new Credits
                {
                    Enabled = false
                },

                Lang = new Lang
                {
                    RangeSelectorFrom = _modelLocalizer["From"].Value,
                    RangeSelectorTo = _modelLocalizer["To"].Value,
                    ViewFullscreen = _modelLocalizer["View in full screen"].Value,
                    PrintChart = _modelLocalizer["Print chart"].Value,
                    DownloadPNG = _modelLocalizer["Download PNG image"].Value,
                    DownloadJPEG = _modelLocalizer["Download JPEG image"].Value,
                    DownloadPDF = _modelLocalizer["Download PDF document"].Value,
                    DownloadSVG = _modelLocalizer["Download SVG vector image"].Value,
                },

                Exporting = new Exporting
                {
                    Buttons = new ExportingButtons
                    {
                        ContextButton = new ExportingButtonsContextButton
                        {
                            Theme = new ExportingButtonsContextButtonTheme
                            {
                                Fill = "none"
                            }
                        }
                    }
                }
            };

            foreach (var x in ViewData.Select((Data, idx) => new { idx, Data }))
            {
                chartOptions.Series.Add(new LineSeries
                {
                    Name = x.Data.Key,
                    Data = x.Data.Value as List<LineSeriesData>,
                    ShowInLegend = true,
                    ShowInNavigator = true,
                    LegendIndex = x.idx
                });
            }

            var renderer = new HighstockRenderer(chartOptions).RenderHtml();

            ViewData["renderer"] = renderer.Replace("var ChartOptions = {", "var ChartOptions = {\"legend\":{\"enabled\": true},");
        }
    }
}