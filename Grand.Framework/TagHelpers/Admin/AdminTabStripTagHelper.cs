﻿using Grand.Framework.Events;
using Grand.Services.Events;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grand.Framework.TagHelpers.Admin
{
    [HtmlTargetElement("admin-tabstrip")]
    public partial class AdminTabStripTagHelper : TagHelper
    {
        private readonly IEventPublisher _eventPublisher;

        public AdminTabStripTagHelper(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }

        [HtmlAttributeName("SetTabPos")]
        public bool SetTabPos { get; set; } = false;

        [HtmlAttributeName("Name")]
        public string Name { get; set; }

        [ViewContext]
        public ViewContext ViewContext { get; set; }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName] = new List<string>();

            var content = await output.GetChildContentAsync();
            var list = (List<string>)ViewContext.ViewData[typeof(AdminTabContentTagHelper).FullName];

            output.TagName = "div";
            output.Attributes.SetAttribute("id", Name);
            output.Attributes.SetAttribute("style", "display:none");
            var rnd = new Random().Next(0, 100);
            var sb = new StringBuilder();
            sb.AppendLine("<script>");
            sb.AppendLine("$(document).ready(function () {");
            sb.AppendLine($"var tab_{rnd} = $('#{Name}').kendoTabStrip({{ ");
            sb.AppendLine($"     tabPosition: '{(SetTabPos ? "left" : "top")}',");
            sb.AppendLine($"     animation: {{ open: {{ effects: 'fadeIn'}} }},");
            sb.AppendLine("     select: tabstrip_on_tab_select");
            sb.AppendLine("  }).data('kendoTabStrip');");
            sb.AppendLine($"$('#{Name}').show();");

            var eventMessage = new AdminTabStripCreated(Name);
            await _eventPublisher.Publish(eventMessage);
            int i = 0;
            foreach (var eventBlock in eventMessage.BlocksToRender)
            {
                i++;
                sb.AppendLine($"tab_{rnd}.append({{");
                sb.AppendLine($"    text: '{eventBlock.tabname}',");
                sb.AppendLine($"    content: '{eventBlock.content}'");
                sb.AppendLine("});");
            }

            sb.AppendLine("})");

            sb.AppendLine("</script>");
            sb.AppendLine($"<input type='hidden' id='selected-tab-index' name='selected-tab-index' value='{GetSelectedTabIndex()}'>");


            output.PostContent.AppendHtml(string.Concat(list));
            output.PostElement.AppendHtml(sb.ToString());
        }

        private int GetSelectedTabIndex()
        {
            //keep this method synchornized with
            //"SetSelectedTabIndex" method of \Administration\Controllers\BaseGrandController.cs
            int index = 0;
            string dataKey = "Grand.selected-tab-index";
            if (ViewContext.ViewData[dataKey] is int)
            {
                index = (int)ViewContext.ViewData[dataKey];
            }
            if (ViewContext.TempData[dataKey] is int)
            {
                index = (int)ViewContext.TempData[dataKey];
            }

            //ensure it's not negative
            if (index < 0)
                index = 0;

            return index;
        }

    }
}
