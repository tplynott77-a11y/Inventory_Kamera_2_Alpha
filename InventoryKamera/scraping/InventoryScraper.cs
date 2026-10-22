using InventoryKamera.game;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace InventoryKamera
{

    public enum InventoryPage
    {
        Weapons,
        Artifacts,
        CharacterDevelopmentItems,
        Food,
        Materials,
        Gadget,
        Quest,
        PreciousItems,
        Furnishings,
    }

    public enum Quality
    {
        INVALID,
        ONESTAR,
        TWOSTAR,
        THREESTAR,
        FOURSTAR,
        FIVESTAR
    }

    internal static class InventoryPageExtension
    {
        public static string ToString(this InventoryPage page)
        {
            switch (page)
            {
                case InventoryPage.CharacterDevelopmentItems:
                    return "CharDevItems";
                default:
                    return page.ToString();
            }
        }
    }

    internal class InventoryScraper
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        protected InventoryPage inventoryPage;

        protected bool SortByLevel = false;

        protected int SortByObtained = 0;

        protected readonly List<InventoryPage> materialPages;

        protected readonly IOcrService ocrService;
        protected readonly IImagePreprocessor imagePreprocessor;
        protected readonly IScanSettings scanSettings;
        protected readonly IScanProgressReporter progressReporter;

        /// <summary>
        /// Shared with every subclass (previously duplicated as a private method on
        /// <c>ArtifactScraper</c> only) so every controller-mode capture region -- including this
        /// base class's own <see cref="DetectCurrentTabIndex"/> -- can leave a visual
        /// trail when <see cref="IScanSettings.LogScreenshots"/> is on, not just artifacts.
        /// <paramref name="relativePath"/> may contain '/' to nest into subfolders (e.g.
        /// <c>"weapons/weapon0/name/name"</c>), matching the per-item folder layout
        /// <c>InventoryKamera.ProcessImageCollectionAsync</c> already uses for cataloguing failures --
        /// so scan attempts saved from here land in the same kind of organized tree instead of a flat
        /// dump of files directly under <c>./logging</c>.
        /// </summary>
        protected void SaveDebugScreenshot(Bitmap bitmap, string relativePath, bool force = false)
        {
            if (!force && !scanSettings.LogScreenshots) return;

            string fullPath = Path.Combine("./logging", relativePath + ".png");
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
            bitmap.Save(fullPath);
        }

        public InventoryScraper(IOcrService ocrService, IImagePreprocessor imagePreprocessor, IScanSettings scanSettings, IScanProgressReporter progressReporter)
        {
            this.ocrService = ocrService;
            this.imagePreprocessor = imagePreprocessor;
            this.scanSettings = scanSettings;
            this.progressReporter = progressReporter;

            materialPages = new List<InventoryPage>();

            materialPages.AddRange(Enum.GetValues(typeof(InventoryPage)).Cast<InventoryPage>());
            materialPages.Remove(InventoryPage.Weapons);
            materialPages.Remove(InventoryPage.Artifacts);
        }

        internal bool StopScanning { get; set; }

        /// <summary>
        /// Parses the item name from an item card's nameplate
        /// </summary>
        /// <param name="nameplate">The item nameplate to parse</param>
        /// <returns>String parsed from nameplate</returns>
        internal string ScanItemName(Bitmap nameplate)
        {
            return ScanItemNameWithConfidence(nameplate).Text;
        }

        /// <summary>
        /// Same recognition as <see cref="ScanItemName"/>, also returning Tesseract's confidence --
        /// used by callers that gate low-confidence item names on inline correction (Phase 3 §3.3).
        /// Not the default <see cref="ScanItemName"/> return shape because one caller
        /// (<c>WeaponScraper.ScanEnchancementOreName</c>) runs on every weapon used as upgrade fodder,
        /// often dozens per scan -- gating correction there would mean a popup for nearly every junk
        /// item fed to a bulk enhancement, so only callers that specifically opt in (the weapon's own
        /// headline name) should request the confidence and act on it.
        /// </summary>
        internal (string Text, float ConfidencePercent) ScanItemNameWithConfidence(Bitmap nameplate)
        {
            GenshinProcesor.SetGamma(0.2, 0.2, 0.2, ref nameplate);
            Bitmap n = imagePreprocessor.ConvertToGrayscale(nameplate);
            imagePreprocessor.SetInvert(ref n);

            // Analyze
            var (rawText, confidence) = ocrService.AnalyzeTextWithConfidence(n, Tesseract.PageSegMode.SingleBlock);
            string text = Regex.Replace(rawText.ToLower(), @"[\W]", string.Empty);

            n.Dispose();

            return (text, confidence * 100);
        }

        /// <summary>
        /// Parses the number of items from for the current inventory page.
        /// </summary>
        /// <remarks>Note: This is primarily for the Weapons, Artifacts, and Furnishing inventories since they have
        /// specific inventory item counts and capacities.
        /// All other inventory pages have a "shared" inventory item counts and capacities. The count
        /// label's position is measured with the coordinate-picker tool (2026-07-03).</remarks>
        /// <param name="inventoryPage">The current inventory page</param>
        /// <returns>The number of unique items for the current inventory</returns>
        /// <exception cref="FormatException">The inventory item count could not be found where it is expected</exception>
        internal int ScanItemCount()
        {
            Rectangle region = new Rectangle(
                x: (int)(0.8865 * Navigation.GetWidth()),
                y: (int)(0.0509 * Navigation.GetHeight()),
                width: (int)(0.0641 * Navigation.GetWidth()),
                height: (int)(0.0269 * Navigation.GetHeight()));

            return ScanItemCountFromRegion(region);
        }

        private int ScanItemCountFromRegion(Rectangle region)
        {
            using (Bitmap countBitmap = Navigation.CaptureRegion(region))
            {
                progressReporter.SetNavigation_Image(countBitmap);

                Bitmap n = imagePreprocessor.ConvertToGrayscale(countBitmap);
                imagePreprocessor.SetContrast(60.0, ref n);
                imagePreprocessor.SetInvert(ref n);

                var (rawText, confidence) = ocrService.AnalyzeTextWithConfidence(n);
                string text = rawText.Trim();
                n.Dispose();

                // Remove any non-numeric and '/' characters
                text = Regex.Replace(text, @"[^0-9/]", string.Empty);

                float confidencePercent = confidence * 100;
                Logger.Debug("{0} item count OCR: rawText=\"{1}\" confidence={2:0.0}% threshold={3}%", inventoryPage, text, confidencePercent, scanSettings.OcrConfidenceThreshold);
                if (string.IsNullOrWhiteSpace(text) || confidencePercent < scanSettings.OcrConfidenceThreshold)
                {
                    Logger.Debug("{0} item count below confidence threshold -- requesting inline correction", inventoryPage);
                    string corrected = progressReporter.RequestCorrection(countBitmap, text, confidencePercent, $"{inventoryPage} item count");
                    text = Regex.Replace(corrected ?? string.Empty, @"[^0-9/]", string.Empty);
                }

                if (string.IsNullOrWhiteSpace(text) || scanSettings.LogScreenshots)
                {
                    SaveInventoryBitmap(countBitmap, "ItemCount.png");
                    SaveInventoryBitmap(Navigation.CaptureWindow(), $"InventoryWindow_{Navigation.GetWidth()}x{Navigation.GetHeight()}.png");
                    if (string.IsNullOrWhiteSpace(text)) throw new FormatException($"Unable to locate {inventoryPage} item count.");
                }

                int count;
                string pageCapacity;
                switch (inventoryPage)
                {
                    case InventoryPage.Artifacts:
                        pageCapacity = "1800";
                        break;
                    default:
                        pageCapacity = "2000";
                        break;
                }

                if (Regex.IsMatch(text, "/")) // Check for slash
                {
                    count = int.Parse(text.Split('/')[0]);
                }
                else if (Regex.Matches(text, pageCapacity).Count == 1) // Remove the inventory capacity from number
                {
                    text = text.Replace(pageCapacity, string.Empty);
                    count = int.Parse(text);
                }
                else // Extreme worst case
                {
                    count = 2000;
                    Logger.Debug("Defaulted to 2000 for inventory page capacity");
                }

                return count;
            }
        }

        /// <summary>
        /// <summary>
        /// Determines the quality of an item based on it's nameplate
        /// </summary>
        /// <param name="nameplate">The an item card's nameplate</param>
        /// <returns>An integer representing quality from 0 - 5 (invalid - 5 star)</returns>
        internal int GetQuality(Bitmap nameplate)
        {
            var avg = imagePreprocessor.AverageColor(nameplate);
            var averageColor = Color.FromArgb((int)avg.R, (int)avg.G, (int)avg.B);

            Color fiveStar = Color.FromArgb(255, 188, 105, 50);
            Color fourStar = Color.FromArgb(255, 161, 86, 224);
            Color threeStar = Color.FromArgb(255, 81, 127, 203);
            Color twoStar = Color.FromArgb(255, 42, 143, 114);
            Color oneStar = Color.FromArgb(255, 114, 119, 138);

            var colors = new List<Color> { Color.Black, oneStar, twoStar, threeStar, fourStar, fiveStar };

            var c = GenshinProcesor.ClosestColor(colors, averageColor);

            return colors.IndexOf(c);
        }

        /// <summary>
        /// Extracts a bitmap copy of an item card's nameplate. Measured with the coordinate-picker
        /// tool (2026-07-03).
        /// </summary>
        internal static Bitmap GetItemNameBitmap(Bitmap card)
        {
            return GenshinProcesor.CopyBitmap(card,
                new Rectangle(
                    x: 0,
                    y: 0,
                    width: card.Width,
                    height: (int)(card.Height * 0.0574)));
        }

        /// <summary>
        /// Extracts a bitmap copy of an item card's equipped character status. Measured with the
        /// coordinate-picker tool (2026-07-03) -- the left edge matters here: an earlier guess that
        /// started far enough left to include the character portrait icon made Tesseract hallucinate
        /// stray characters even though the text itself was clean.
        /// </summary>
        internal static Bitmap GetEquippedBitmap(Bitmap card)
        {
            return GenshinProcesor.CopyBitmap(card,
                new Rectangle(
                    x: (int)(card.Width * 0.1527),
                    y: (int)(card.Height * 0.9444),
                    width: (int)(card.Width * 0.8309),
                    height: (int)(card.Height * 0.0519)));
        }

        // Genshin's inventory remembers whichever tab was last open, so tab-switching can't assume a
        // known starting tab -- the tab name label gets OCR'd and fuzzy-matched against this list.
        // Shared across Weapons/Artifacts/Character Development Items (Phase 3 §6c).
        internal static readonly string[] ControllerInventoryTabNames =
        {
            "Weapons", "Artifacts", "Character Development Items", "Food", "Materials",
            "Gadget", "Quest", "Precious Items", "Furnishings",
        };

        /// <summary>
        /// Captures the selected item's always-visible detail card -- confirmed by the user to be the
        /// same region for Weapons, Artifacts, and Character Development Items. Percentages measured
        /// with the coordinate-picker tool from a full-window capture (2026-07-03, 1920x1080).
        /// </summary>
        internal Bitmap GetItemCard()
        {
            return Navigation.CaptureRegion(
                x: (int)(0.7031 * Navigation.GetWidth()),
                y: (int)((Navigation.IsNormal ? 0.1231 : 0.1120) * Navigation.GetHeight()),
                width: (int)(0.2167 * Navigation.GetWidth()),
                height: (int)((Navigation.IsNormal ? 0.7556 : 0.7800) * Navigation.GetHeight()));
        }
        /// <summary>
        /// Opens the pause menu and navigates into Inventory via controller -- ported from the
        /// live-verified sequence in <c>ControllerNavigationTests</c> (2026-07-05): open menu, move
        /// down twice (Inventory is at grid position [0,2]), confirm with B (Genshin's confirm button
        /// -- swapped from standard Xbox convention, A is back/cancel).
        /// </summary>
        internal void EnterInventory(GameController controller)
        {
            // The mouse-based scan's Navigation.InventoryScreen() started every phase with a real
            // Escape press to guarantee a known baseline (unpaused, no menu open) before doing
            // anything else -- controller-mode ad-hoc tests never needed this because the user
            // manually alt-tabbed into that same baseline state before clicking a test. A real scan
            // can't assume that: Genshin might already be sitting in a menu (paused, or left open by
            // a previous phase) when this runs, and EnterControllerMode()'s stick-nudge+A sequence
            // only reliably flips the input scheme from the free-roam state, not from inside a menu.
            Navigation.sim.Keyboard.KeyPress(Navigation.escapeKey);
            Navigation.SystemWait(Navigation.Speed.UI);

            // Per user (2026-07-04): "make sure the speed is hooked up" -- these were still fixed
            // regardless of the Fast/Normal/Slow setting, unlike every per-item timing elsewhere. This
            // is one-time setup (runs once per scan phase, not per item), so it's a much smaller share
            // of a full scan's total time than the per-item loop -- being generous here is nearly free.
            // Doubled again (2026-07-05) after a live tab-detection miss right after this sequence
            // ("Weapons" misread as "eepons |") -- the menu navigation itself was too fast and
            // occasionally not fully settled before the next step read the screen.
            controller.EnterControllerMode();
            Thread.Sleep(ScaledControllerDelay(2000));
            controller.OpenMenu();
            Thread.Sleep(ScaledControllerDelay(2000));
            controller.Move(GameController.MenuDirection.Down, 2, holdMs: ScaledControllerDelay(300), settleMs: ScaledControllerDelay(300));
            Thread.Sleep(ScaledControllerDelay(600));
            controller.TapButton(Xbox360Button.B, holdMs: ScaledControllerDelay(300));
            // Per user (2026-07-04): confirming into Inventory plays a screen-transition animation
            // that outlasts the plain-scaled wait -- unlike input-registration timing, an animation's
            // real duration doesn't shrink just because Fast wants quicker input pacing. Floored flat
            // regardless of speed setting (doubled 2026-07-05, same reasoning as above); this is a
            // one-time per-scan cost, so being generous here is nearly free even though the same floor
            // was rejected for the per-item advance wait.
            Thread.Sleep(Math.Max(3000, ScaledControllerDelay(2000)));
        }

        /// <summary>
        /// Captures the tab-name label (top-left of the inventory screen), OCRs it, and fuzzy-matches
        /// it against <see cref="ControllerInventoryTabNames"/>. Returns the matched index (-1 if no
        /// confident match). Must be called while already inside Inventory (see
        /// <see cref="EnterInventory"/>) and before the owning <c>GameController</c> is
        /// disposed -- disposal mashes back out of the menu as a safety net.
        /// </summary>
        internal int DetectCurrentTabIndex(out string rawText)
        {
            // Height tightened 0.08 -> 0.05 (2026-07-07): the debug screenshot showed the sub-tab
            // icon row bleeding into the bottom of the crop, which likely confused Tesseract's
            // SingleLine page-seg mode into garbling otherwise-clean "Weapons" text into "Mn NT".
            // Needs live confirmation that the tab-name line itself is still fully inside the crop.
            using (var region = Navigation.CaptureRegion(
                x: (int)(0.09 * Navigation.GetWidth()),
                y: (int)(0.035 * Navigation.GetHeight()),
                width: (int)(0.20 * Navigation.GetWidth()),
                height: (int)(0.05 * Navigation.GetHeight())))
            {
                SaveDebugScreenshot(region, "tabdetection/region");

                var preprocessor = new ImageProcessor();
                Bitmap processed = preprocessor.ConvertToGrayscale(region);
                preprocessor.SetContrast(60.0, ref processed);
                preprocessor.SetInvert(ref processed);

                SaveDebugScreenshot(processed, "tabdetection/processed");

                using (processed)
                using (var ocr = new OcrService())
                {
                    rawText = ocr.AnalyzeText(processed, Tesseract.PageSegMode.SingleLine).Trim();
                }
            }

            var normalizedTabs = ControllerInventoryTabNames.Select(t => t.ToLower().Replace(" ", "")).ToArray();
            string normalizedText = Regex.Replace(rawText.ToLower(), @"[\W]", string.Empty);
            if (normalizedText.Length < 4)
            {
                Logger.Warn("V4 tab OCR rejected empty/short heading: '{0}'", rawText);
                return -1;
            }

            string matchedNormalized = TextNormalizer.FindClosestInList(normalizedText, new HashSet<string>(normalizedTabs));
            if (string.IsNullOrWhiteSpace(matchedNormalized))
                return -1;

            // The alpha accepted ANY nearest match, including gibberish. Require the
            // heading to resemble the proposed tab before allowing navigation.
            int editDistance = TabHeadingDistance(normalizedText, matchedNormalized);
            int tolerance = Math.Max(2, matchedNormalized.Length / 3);
            if (editDistance > tolerance)
            {
                Logger.Warn("V4 tab OCR rejected weak match: raw='{0}', candidate='{1}', distance={2}, tolerance={3}",
                    rawText, matchedNormalized, editDistance, tolerance);
                return -1;
            }

            return Array.IndexOf(normalizedTabs, matchedNormalized);
        }

        /// <summary>
        /// Switches to <paramref name="targetTab"/> from wherever the cursor currently is, cycling
        /// LB/RB (the inventory sub-tab row's own input, distinct from the pause menu's stick-driven
        /// grid). Tabs wrap around (circular) -- takes whichever direction is fewer presses. Must run
        /// inside Inventory already (see <see cref="EnterInventory"/>).
        /// </summary>
        /// <param name="knownCurrentTab">
        /// If the caller already knows which tab is active (e.g. the previous scan phase just
        /// finished switching to and scanning that exact tab, and nothing changes tabs mid-scan), pass
        /// it here to skip OCR detection entirely. Per user (2026-07-05): re-detecting via OCR for
        /// every phase transition is both slower and less reliable than just remembering where the
        /// last phase left off -- a live miss ("Weapons" misread as "eepons J"/"eepons |") persisted
        /// even after a settle-wait and 3 retry attempts, pointing at an OCR/rendering issue with this
        /// specific capture rather than a timing one; skipping the read avoids it outright.
        /// </param>
        /// <returns>The tab actually active after this call (<paramref name="targetTab"/> on success,
        /// or the original tab if detection failed and the switch was skipped) -- pass this into the
        /// next phase's <paramref name="knownCurrentTab"/> to keep the chain going without OCR.</returns>
        // V4 milestone 1: navigation must prove the destination before any scan
        // starts. Last phase's currentTab is only a hint, NEVER ground truth.
        internal string SwitchToTab(GameController controller, string targetTab, string knownCurrentTab = null)
        {
            int targetIndex = Array.IndexOf(ControllerInventoryTabNames, targetTab);
            if (targetIndex < 0)
                return AbortUnverifiedTab("Unrecognised inventory destination: " + targetTab);

            Thread.Sleep(ScaledControllerDelay(350));

            string rawText;
            int currentIndex = DetectStableTabIndex(out rawText);
            int tabCount = ControllerInventoryTabNames.Length;

            if (currentIndex < 0)
            {
                Logger.Warn("V4: Cannot identify initial tab (OCR='{0}'); sweeping right to recover.", rawText);
                for (int attempt = 0; attempt < tabCount && currentIndex < 0; attempt++)
                {
                    controller.TapButton(Xbox360Button.RightShoulder, holdMs: ScaledControllerDelay(180));
                    Thread.Sleep(ScaledControllerDelay(650));
                    currentIndex = DetectStableTabIndex(out rawText);
                }
            }

            if (currentIndex < 0)
                return AbortUnverifiedTab("Cannot identify the current inventory tab after a bounded search (OCR='" + rawText + "').");

            string actualTab = ControllerInventoryTabNames[currentIndex];
            if (knownCurrentTab != null && !string.Equals(knownCurrentTab, actualTab, StringComparison.Ordinal))
                Logger.Warn("V4 corrected stale previous-tab hint: expected '{0}', observed '{1}'.",
                    knownCurrentTab, actualTab);

            if (currentIndex != targetIndex)
            {
                int forward = (targetIndex - currentIndex + tabCount) % tabCount;
                int backward = (currentIndex - targetIndex + tabCount) % tabCount;
                bool moveRight = forward <= backward;
                int steps = Math.Min(forward, backward);
                Xbox360Button button = moveRight ? Xbox360Button.RightShoulder : Xbox360Button.LeftShoulder;

                Logger.Info("V4 navigating {0} -> {1}: {2} {3} presses.",
                    actualTab, targetTab, steps, moveRight ? "right" : "left");

                for (int i = 0; i < steps; i++)
                {
                    controller.TapButton(button, holdMs: ScaledControllerDelay(200));
                    Thread.Sleep(ScaledControllerDelay(800));
                }
                Thread.Sleep(ScaledControllerDelay(550));
            }

            // NEVER infer success from a button press. Confirm the real heading.
            int observedIndex = DetectStableTabIndex(out rawText);
            if (observedIndex == targetIndex)
            {
                Logger.Info("V4 verified target inventory tab: '{0}' (OCR='{1}').", targetTab, rawText);
                return targetTab;
            }

            Logger.Warn("V4 tab switch did not verify: expected '{0}', observed '{1}' (OCR='{2}'). Searching.",
                targetTab,
                observedIndex >= 0 ? ControllerInventoryTabNames[observedIndex] : "unknown",
                rawText);

            // Bounded recovery: visit each next tab, read back the heading, and
            // stop at the first independently verified destination.
            for (int attempt = 0; attempt < tabCount; attempt++)
            {
                controller.TapButton(Xbox360Button.RightShoulder, holdMs: ScaledControllerDelay(200));
                Thread.Sleep(ScaledControllerDelay(700));
                observedIndex = DetectStableTabIndex(out rawText);
                if (observedIndex == targetIndex)
                {
                    Logger.Info("V4 recovered and verified '{0}' in {1} additional tab steps.",
                        targetTab, attempt + 1);
                    return targetTab;
                }
            }

            return AbortUnverifiedTab("Cannot reach/verify inventory tab '" + targetTab +
                "' after a bounded navigation attempt (last OCR='" + rawText + "').");
        }

        // Require two agreeing heading reads. We intentionally pay this small
        // one-time cost once per category to avoid scanning the wrong inventory.
        private int DetectStableTabIndex(out string lastRawText)
        {
            lastRawText = "";
            int previousIndex = -1;
            for (int attempt = 0; attempt < 4; attempt++)
            {
                string raw;
                int currentIndex = DetectCurrentTabIndex(out raw);
                lastRawText = raw;
                if (currentIndex >= 0 && currentIndex == previousIndex)
                    return currentIndex;

                previousIndex = currentIndex;
                if (attempt < 3)
                    Thread.Sleep(ScaledControllerDelay(170));
            }
            return -1;
        }

        private string AbortUnverifiedTab(string message)
        {
            Logger.Error("V4 navigation aborted: {0}", message);
            progressReporter.AddError("V4 navigation aborted: " + message);
            // A later category must not trust the wrong assumed current tab.
            // This cancellation is read by all category scan loops and phase guards.
            GameScanner.CancelRequested = true;
            try
            {
                using (Bitmap failed = Navigation.CaptureWindow())
                    SaveDebugScreenshot(failed, "navigation/unverified_inventory_tab", force: true);
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Unable to capture V4 navigation-failure screenshot.");
            }
            throw new InvalidOperationException(message);
        }

        private static int TabHeadingDistance(string a, string b)
        {
            int[] previous = new int[b.Length + 1];
            int[] current = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) previous[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                current[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int substitutionCost = a[i - 1] == b[j - 1] ? 0 : 1;
                    current[j] = Math.Min(Math.Min(current[j - 1] + 1,
                        previous[j] + 1), previous[j - 1] + substitutionCost);
                }
                int[] swap = previous;
                previous = current;
                current = swap;
            }
            return previous[b.Length];
        }

        /// <summary>
        /// Scales a base controller-timing duration by the app's existing scan-speed setting
        /// (<see cref="Navigation.GetDelay"/> -- the same 0.5x/Fast, 1x/Normal, 1.5x/Slow multiplier
        /// already applied to every mouse-path scan timing via <see cref="Navigation.SystemWait(Speed)"/>),
        /// so per-item controller-scan timing speeds up/slows down consistently with the rest of the
        /// app instead of being hardcoded. Below 1x (Fast), the multiplier is squared rather than
        /// applied once -- per direct user feedback (2026-07-04), a plain 0.5x scale of an
        /// already-tuned-fast base wasn't fast enough. A 20ms floor guards against a degenerate
        /// near-zero hold if the underlying setting is ever extended below 0.5x. UNVERIFIED how low
        /// Fast's resulting values can go and still register reliably against the game -- this is new,
        /// live-testing-only territory, not something a build/compile check can confirm.
        /// </summary>
        internal static int ScaledControllerDelay(int baseMs)
        {
            double delay = Navigation.GetDelay();
            if (delay < 1) delay *= delay;
            return Math.Max(20, (int)(baseMs * delay));
        }

        internal void SaveInventoryBitmap(Bitmap image, string filename)
        {
            var path = "./logging/";

            if (materialPages.Contains(inventoryPage))
                path += "Materials/";
            else path += inventoryPage.ToString() + "/";

            // filename may itself contain '/' to nest into a subfolder (e.g. "quantity/Iron_Quantity.png")
            // -- GatherData only creates the top-level per-page folder (./logging/materials/) up front,
            // so any subfolder needs creating here rather than assuming it already exists.
            Directory.CreateDirectory(Path.GetDirectoryName(path + filename));
            image.Save(path + filename);
        }

    }
}