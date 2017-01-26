using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Collections.Generic;
using System;

namespace NUnit.Test
{
    public class TrackerOpened : Tracker
    {
        public List<String> GetQueue()
        {
            return base.queue;
        }
    }

    public class TrackerTest
    {

        [Test]
        public void EditorTest()
        {

            TrackerOpened t = new TrackerOpened();
            t.Start();
            t.ActionTrace("Verbo", "Type", "ID");

            string traceWithoutTimestamp = t.GetQueue()[0].Substring(t.GetQueue()[0].IndexOf(',') + 1);

            Assert.AreEqual(traceWithoutTimestamp, "Verbo,Type,ID");

        }
    }
}
