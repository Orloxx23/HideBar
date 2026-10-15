using System.Collections.Generic;
using HideBar.Core;

namespace HideBar.Tests
{
    static class TaskbarControllerTests
    {
        public static void Register()
        {
            TestRunner.Add("hides the taskbar only on the picked display", HidesOnlyThePickedDisplay);
            TestRunner.Add("gives the space to maximized windows without broadcasting", GivesTheSpaceWithoutBroadcasting);
            TestRunner.Add("leaves the work area alone when reclaiming space is off", LeavesTheWorkAreaAloneWhenReclaimingIsOff);
            TestRunner.Add("hides the taskbar again when Explorer shows it", HidesTheTaskbarAgain);
            TestRunner.Add("unpicking a display brings its taskbar and space back", UnpickingRestoresTheDisplay);
            TestRunner.Add("pausing restores everything and resuming hides again", PausingAndResuming);
            TestRunner.Add("the main display can't be picked", TheMainDisplayCannotBePicked);
            TestRunner.Add("restoring with auto-hide gives the whole screen back", RestoringWithAutoHide);
            TestRunner.Add("numbers displays from left to right", NumbersDisplaysFromLeftToRight);
            TestRunner.Add("reports picked displays that aren't connected", ReportsMissingDisplays);
            TestRunner.Add("reports the state of each taskbar", ReportsTaskbarStates);
            TestRunner.Add("saves every settings change", SavesSettingsChanges);
        }

        // Three displays laid out like the author's desk: a 4K monitor on the left, the main
        // 1080p monitor in the middle and a 7" portrait screen on the right.
        class Desk
        {
            public readonly FakeDisplaySystem System = new FakeDisplaySystem();
            public readonly MemorySettingsStore Store = new MemorySettingsStore();
            public readonly FakeStartup Startup = new FakeStartup();
            public readonly Display Wide;
            public readonly Display Main;
            public readonly Display Side;

            public Desk(params string[] picked)
            {
                Main = System.AddDisplay(2, "MAIN", new Rect(0, 0, 1920, 1080), true);
                Side = System.AddDisplay(3, "SIDE", new Rect(1920, 54, 2520, 1078), false);
                Wide = System.AddDisplay(1, "WIDE", new Rect(-3840, -946, 0, 1214), false);
                foreach (string id in picked) Store.Stored.MonitorIds.Add(id);
            }

            public TaskbarController CreateController()
            {
                return new TaskbarController(System, Store, Startup);
            }

            public bool TaskbarVisible(Display display)
            {
                return System.TaskbarOf(display).IsVisible;
            }

            public Rect WorkAreaWithTaskbar(Display display)
            {
                return display.Bounds.Without(System.TaskbarOf(display).Bounds);
            }
        }

        static Display Find(TaskbarController controller, string modelId)
        {
            return controller.GetDisplays().Find(display => display.ModelId == modelId);
        }

        static void HidesOnlyThePickedDisplay()
        {
            var desk = new Desk("SIDE");
            desk.CreateController().Apply();

            Assert.False(desk.TaskbarVisible(desk.Side), "side taskbar visible");
            Assert.True(desk.TaskbarVisible(desk.Main), "main taskbar visible");
            Assert.True(desk.TaskbarVisible(desk.Wide), "wide taskbar visible");
        }

        static void GivesTheSpaceWithoutBroadcasting()
        {
            var desk = new Desk("SIDE");
            desk.CreateController().Apply();

            Assert.Equal(desk.Side.Bounds, desk.Side.WorkArea, "side work area");
            Assert.True(desk.System.Calls.Contains("workarea " + desk.Side.Bounds), "work area set without broadcast");
        }

        static void LeavesTheWorkAreaAloneWhenReclaimingIsOff()
        {
            var desk = new Desk("SIDE");
            desk.Store.Stored.ReclaimSpace = false;
            Rect before = desk.Side.WorkArea;

            desk.CreateController().Apply();

            Assert.False(desk.TaskbarVisible(desk.Side), "side taskbar visible");
            Assert.Equal(before, desk.Side.WorkArea, "side work area");
        }

        static void HidesTheTaskbarAgain()
        {
            var desk = new Desk("SIDE");
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            desk.System.TaskbarOf(desk.Side).IsVisible = true;
            controller.Apply();

            Assert.False(desk.TaskbarVisible(desk.Side), "side taskbar visible");
        }

        static void UnpickingRestoresTheDisplay()
        {
            var desk = new Desk("SIDE");
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            controller.SetPicked(Find(controller, "SIDE"), false);

            Assert.True(desk.TaskbarVisible(desk.Side), "side taskbar visible");
            Assert.Equal(desk.WorkAreaWithTaskbar(desk.Side), desk.Side.WorkArea, "side work area");
            Assert.True(desk.System.Calls.Contains("workarea " + desk.Side.WorkArea + " broadcast"), "work area broadcast");
            Assert.False(desk.Store.Stored.MonitorIds.Contains("SIDE"), "SIDE still saved");
        }

        static void PausingAndResuming()
        {
            var desk = new Desk("SIDE");
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            controller.SetPaused(true);
            Assert.True(desk.TaskbarVisible(desk.Side), "side taskbar visible while paused");
            Assert.Equal(desk.WorkAreaWithTaskbar(desk.Side), desk.Side.WorkArea, "side work area while paused");
            Assert.Equal(HidingStatus.Paused, controller.Status, "status while paused");

            controller.Apply();
            Assert.True(desk.TaskbarVisible(desk.Side), "side taskbar visible after a tick while paused");

            controller.SetPaused(false);
            Assert.False(desk.TaskbarVisible(desk.Side), "side taskbar visible after resuming");
        }

        static void TheMainDisplayCannotBePicked()
        {
            var desk = new Desk();
            TaskbarController controller = desk.CreateController();

            controller.SetPicked(Find(controller, "MAIN"), true);

            Assert.False(desk.Store.Stored.MonitorIds.Contains("MAIN"), "MAIN saved");
            Assert.True(desk.TaskbarVisible(desk.Main), "main taskbar visible");
        }

        static void RestoringWithAutoHide()
        {
            var desk = new Desk("SIDE");
            desk.System.AutoHide = true;
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            controller.SetPaused(true);

            Assert.True(desk.TaskbarVisible(desk.Side), "side taskbar visible");
            Assert.Equal(desk.Side.Bounds, desk.Side.WorkArea, "side work area");
        }

        static void NumbersDisplaysFromLeftToRight()
        {
            var desk = new Desk();
            List<Display> displays = desk.CreateController().GetDisplays();

            Assert.Equal("WIDE", displays[0].ModelId, "display 1");
            Assert.Equal("MAIN", displays[1].ModelId, "display 2");
            Assert.Equal("SIDE", displays[2].ModelId, "display 3");
            Assert.Equal(3, displays[2].Number, "number of the rightmost display");
        }

        static void ReportsMissingDisplays()
        {
            var desk = new Desk("UNPLUGGED");
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            Assert.Equal(HidingStatus.NotConnected, controller.Status, "status");
        }

        static void ReportsTaskbarStates()
        {
            var desk = new Desk("SIDE");
            TaskbarController controller = desk.CreateController();
            controller.Apply();

            Assert.Equal(TaskbarState.MainDisplay, controller.GetTaskbarState(Find(controller, "MAIN")), "main");
            Assert.Equal(TaskbarState.Hidden, controller.GetTaskbarState(Find(controller, "SIDE")), "side");
            Assert.Equal(TaskbarState.Visible, controller.GetTaskbarState(Find(controller, "WIDE")), "wide");
            Assert.Equal(HidingStatus.Hiding, controller.Status, "status");
            Assert.Equal(1, controller.HiddenDisplayCount, "hidden display count");

            controller.SetPaused(true);
            Assert.Equal(TaskbarState.Paused, controller.GetTaskbarState(Find(controller, "SIDE")), "side while paused");
        }

        static void SavesSettingsChanges()
        {
            var desk = new Desk();
            TaskbarController controller = desk.CreateController();

            controller.SetPicked(Find(controller, "WIDE"), true);
            Assert.True(desk.Store.Stored.MonitorIds.Contains("WIDE"), "WIDE saved");

            controller.SetReclaimSpace(false);
            Assert.False(desk.Store.Stored.ReclaimSpace, "reclaimSpace saved");

            controller.SetShowTrayIcon(false);
            Assert.False(desk.Store.Stored.ShowTrayIcon, "showTrayIcon saved");

            controller.SetStartWithWindows(true);
            Assert.True(desk.Startup.IsEnabled, "start with Windows");
        }
    }
}
