using System;
using System.Linq;
using System.Text.Json.Nodes;
using DiskSpaceMonitor.Widgets;
using DiskSpaceMonitor.Widgets.Circular;
using FluentAssertions;

namespace DiskSpaceMonitor.UnitTests.Widgets
{
    [TestFixture]
    public class WidgetRegistryTests
    {
        // A minimal second factory so we can test ordering, lookup and fallback without WPF.
        private sealed class FakeFactory : IWidget
        {
            public string Id => "Fake";
            public string DisplayName => "Fake style";
            public IWidgetView CreateView() => throw new NotSupportedException();
            public IWidgetConfig DefaultConfig() => throw new NotSupportedException();
            public IWidgetConfig ReadConfig(JsonNode? json) => throw new NotSupportedException();
            public JsonNode WriteConfig(IWidgetConfig config) => throw new NotSupportedException();
            public IWidgetConfigEditor CreateEditor(IWidgetConfig initial, Action onChanged) => throw new NotSupportedException();
        }

        private static WidgetRegistry Build() => new(new CircularWidget(), new FakeFactory());

        [Test]
        public void All_PreservesRegistrationOrder()
        {
            Build().All.Select(f => f.Id).Should().Equal("Circular", "Fake");
        }

        [Test]
        public void Get_KnownId_ReturnsThatFactory()
        {
            var registry = Build();

            registry.Get("Fake").Id.Should().Be("Fake");
            registry.Get("Circular").Id.Should().Be("Circular");
        }

        [Test]
        public void Get_UnknownId_FallsBackToDefault()
        {
            Build().Get("does-not-exist").Id.Should().Be(WidgetRegistry.DefaultWidgetId);
        }

        [Test]
        public void Get_Null_FallsBackToDefault()
        {
            Build().Get(null).Id.Should().Be(WidgetRegistry.DefaultWidgetId);
        }

        [Test]
        public void Contains_ReflectsRegistration()
        {
            var registry = Build();

            registry.Contains("Fake").Should().BeTrue();
            registry.Contains("nope").Should().BeFalse();
        }
    }
}
