﻿using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Xunit;

namespace Celestial.UIToolkit.Tests
{

    public class ItemsSourceCollectionTests
    {
        
        [Fact]
        public void IsUsingItemsSourceGetsSet()
        {
            var itemsSource = CreateIntItemsSource(1);
            var emptyCollection = new ItemsSourceCollection();
            Assert.False(emptyCollection.IsUsingItemsSource);

            emptyCollection.ItemsSource = itemsSource;
            Assert.True(emptyCollection.IsUsingItemsSource);
        }

        [Fact]
        public void CanAddItemsNormally()
        {
            var collection = new ItemsSourceCollection();
            int itemToAdd = 1;
            collection.Add(itemToAdd);
            Assert.Contains(itemToAdd, collection);
        }

        [Fact]
        public void ProvidesItemsViaItemsSource()
        {
            var itemsSource = CreateIntItemsSource(5);
            var collection = new ItemsSourceCollection();

            collection.ItemsSource = itemsSource;
            Assert.True(collection.Cast<int>().SequenceEqual(itemsSource));
        }

        [Fact]
        public void ThrowsWhenSwitchingItemsSourceInFilledCollection()
        {
            var itemsSource = CreateIntItemsSource(1);
            var collection = new ItemsSourceCollection() { 1, 2, 3 };

            var ex = Record.Exception(() => collection.ItemsSource = itemsSource);
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public void ThrowsWhenModifyingCollectionWithItemsSource()
        {
            var itemsSource = CreateIntItemsSource(5);
            var collection = new ItemsSourceCollection();

            collection.ItemsSource = itemsSource;
            TestForMethod(() => collection.Add(0));
            TestForMethod(() => collection.Remove(0));
            TestForMethod(() => collection.RemoveAt(0));
            TestForMethod(() => collection.Clear());
            Assert.True(collection.Cast<int>().SequenceEqual(itemsSource));

            void TestForMethod(Action method)
            {
                var ex = Record.Exception(method);
                Assert.NotNull(ex);
                Assert.IsType<InvalidOperationException>(ex);
            }
        }

        #region Changed Events

        [Fact]
        public void DefaultCollectionModificationRaisesChangeEvents()
        {
            var collection = new ItemsSourceCollection();

            // We want to test that the following methods raise the 
            // PropertyChanged/CollectionChanged events.
            TestChangeEventsForAction(() => collection.Add(1));
            TestChangeEventsForAction(() =>
            {
                collection.Add(0);
                collection.Remove(0);
            });
            TestChangeEventsForAction(() =>
            {
                collection.Add(0);
                collection.RemoveAt(0);
            });
            TestChangeEventsForAction(() => collection.Clear());

            // Checks if the *Changed events are raised when executing changeAction().
            void TestChangeEventsForAction(Action changeAction)
            {
                Assert.PropertyChanged(
                    collection,
                    nameof(ItemsSourceCollection.Count),
                    changeAction);
                Assert.PropertyChanged(
                    collection,
                    "Items[]",
                    changeAction);
                AssertCollectionChanged(
                    collection,
                    changeAction);
            }
        }

        [Fact]
        public void ItemsSourceCanRaiseCollectionChanged()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateObservableIntItemsSource(5);

            collection.ItemsSource = itemsSource;
            AssertCollectionChanged(
                collection,
                () => itemsSource.Add(1));
        }

        [Fact]
        public void SwitchingToEnumerableItemsSourceRaisesChangeEvents()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);

            AssertCollectionChanged(
                collection,
                () => collection.ItemsSource = itemsSource);
            AssertPropertiesChanged(
                collection,
                () => collection.ItemsSource = itemsSource,
                nameof(ItemsSourceCollection.ItemsSource),
                nameof(ItemsSourceCollection.IsUsingItemsSource),
                nameof(ItemsSourceCollection.IsReadOnly),
                nameof(ItemsSourceCollection.IsFixedSize),
                nameof(ItemsSourceCollection.SyncRoot));
        }

        [Fact]
        public void SwitchingToNonEnumerableItemsSourceRaisesChangeEvents()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();

            AssertCollectionChanged(
                collection,
                () => collection.ItemsSource = itemsSource);
            AssertPropertiesChanged(
                collection,
                () => collection.ItemsSource = itemsSource,
                nameof(ItemsSourceCollection.ItemsSource),
                nameof(ItemsSourceCollection.IsUsingItemsSource),
                nameof(ItemsSourceCollection.IsReadOnly),
                nameof(ItemsSourceCollection.IsFixedSize),
                nameof(ItemsSourceCollection.SyncRoot));
        }

        private void AssertPropertiesChanged(
            INotifyPropertyChanged obj,
            Action raiseEvent, 
            params string[] propertyNames)
        {
            foreach (var propertyName in propertyNames)
            {
                Assert.PropertyChanged(obj, propertyName, raiseEvent);
            }
        }

        private void AssertCollectionChanged(
            INotifyCollectionChanged obj,
            Action raiseEvent)
        {
            bool wasRaised = false;
            NotifyCollectionChangedEventHandler handler = (sender, e) =>
            {
                wasRaised = true;
            };

            obj.CollectionChanged += handler;
            raiseEvent();
            obj.CollectionChanged -= handler;
            Assert.True(wasRaised);
        }

        #endregion

        #region Collection[index]

        [Fact]
        public void IndexAccessorWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection();
            int itemToAdd = 1;

            collection.Add(0); // Placeholder, so that the index exists.
            collection[0] = itemToAdd;

            Assert.Equal(itemToAdd, collection[0]);
        }

        [Fact]
        public void IndexAccessorWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(1);

            collection.ItemsSource = itemsSource;
            var firstValue = collection[0];
            var ex = Record.Exception(() => collection[0] = 1); // Prop. should be read-only with an ItemsSource.

            Assert.Equal(firstValue, itemsSource.First());
            Assert.NotNull(ex);
            Assert.IsType<InvalidOperationException>(ex);
        }

        [Fact]
        public void IndexAccessorWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();

            collection.ItemsSource = itemsSource;
            var retrievedValue = collection[0];
            var outOfRangeEx = Record.Exception(() => collection[1]);
            var readOnlyEx = Record.Exception(() => collection[0] = "Unsettable");

            Assert.Same(itemsSource, retrievedValue);
            Assert.NotNull(outOfRangeEx);
            Assert.IsType<IndexOutOfRangeException>(outOfRangeEx);
            Assert.NotNull(readOnlyEx);
            Assert.IsType<InvalidOperationException>(readOnlyEx);
        }

        #endregion

        #region Count

        [Fact]
        public void CountWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection()
            { 1, 2, 3, 4, 5 };

            Assert.Equal(5, collection.Count);
        }

        [Fact]
        public void CountWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);

            collection.ItemsSource = itemsSource;
            Assert.Equal(itemsSource.Length, collection.Count);
        }

        [Fact]
        public void CountWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();

            collection.ItemsSource = itemsSource;

            var count = collection.Count();
            Assert.Single(collection);
        }

        #endregion

        #region IndexOf

        [Fact]
        public void IndexOfWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection()
            { 1, 2, 3, 4, 5 };

            for (int i = 0; i < collection.Count; i++)
            {
                int currentItem = (int)collection[i];
                Assert.Equal(i, collection.IndexOf(currentItem));
            }
            Assert.Equal(-1, collection.IndexOf(-1));
        }

        [Fact]
        public void IndexOfWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);

            collection.ItemsSource = itemsSource;
            for (int i = 0; i < collection.Count; i++)
            {
                int currentItem = (int)collection[i];
                Assert.Equal(i, collection.IndexOf(currentItem));
            }
            Assert.Equal(-1, collection.IndexOf(-1));
        }

        [Fact]
        public void IndexOfWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();

            collection.ItemsSource = itemsSource;
            Assert.Equal(0, collection.IndexOf(itemsSource));
            Assert.Equal(-1, collection.IndexOf(new object()));
        }

        #endregion

        #region Contains

        [Fact]
        public void ContainsWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection()
            { 1, 2, 3, 4, 5 };

            for (int i = 0; i < collection.Count; i++)
            {
                int currentItem = (int)collection[i];
                Assert.True(collection.Contains(currentItem));
            }
            Assert.False(collection.Contains(-1));
        }

        [Fact]
        public void ContainsWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);

            collection.ItemsSource = itemsSource;
            for (int i = 0; i < collection.Count; i++)
            {
                int currentItem = (int)collection[i];
                Assert.True(collection.Contains(currentItem));
            }
            Assert.False(collection.Contains(-1));
        }

        [Fact]
        public void ContainsWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();

            collection.ItemsSource = itemsSource;
            Assert.True(collection.Contains(itemsSource));
            Assert.False(collection.Contains(new object()));
        }

        #endregion

        #region CopyTo

        [Fact]
        public void CopyToWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection()
            { 1, 2, 3, 4, 5 };
            int[] dest = new int[collection.Count + 1];

            collection.CopyTo(dest, 1);
            Assert.Equal(0, dest[0]);
            Assert.True(collection.Cast<int>().SequenceEqual(dest.Skip(1)));
        }

        [Fact]
        public void CopyToWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);
            int[] dest = new int[itemsSource.Length + 1];

            collection.ItemsSource = itemsSource;
            collection.CopyTo(dest, 1);

            Assert.Equal(0, dest[0]);
            Assert.True(collection.Cast<int>().SequenceEqual(dest.Skip(1)));
        }

        [Fact]
        public void CopyToWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();
            var dest = new object[3];

            collection.ItemsSource = itemsSource;
            collection.CopyTo(dest, 1);

            Assert.Equal(itemsSource, dest[1]);
            Assert.Null(dest[0]);
            Assert.Null(dest[2]);
        }

        #endregion

        #region GetEnumerator

        [Fact]
        public void GetEnumeratorWorksWithDefaultCollection()
        {
            var collection = new ItemsSourceCollection();
            var items = new int[] { 1, 2, 3, 4, 5 };
            IEnumerator collectionEnumerator;
            IEnumerator itemsEnumerator;

            foreach (var item in items)
                collection.Add(item);
            collectionEnumerator = collection.GetEnumerator();
            itemsEnumerator = items.GetEnumerator();

            TestEnumeratorEquality(collectionEnumerator, itemsEnumerator);
        }

        [Fact]
        public void GetEnumeratorWorksWithItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateIntItemsSource(5);
            IEnumerator collectionEnumerator;
            IEnumerator itemsEnumerator;

            collection.ItemsSource = itemsSource;
            collectionEnumerator = collection.GetEnumerator();
            itemsEnumerator = itemsSource.GetEnumerator();

            TestEnumeratorEquality(collectionEnumerator, itemsEnumerator);
        }

        [Fact]
        public void GetEnumeratorWorksWithNonEnumerableItemsSource()
        {
            var collection = new ItemsSourceCollection();
            var itemsSource = CreateNonEnumerableItemsSource();
            IEnumerator enumerator;

            collection.ItemsSource = itemsSource;
            enumerator = collection.GetEnumerator();

            var enumerationNotStartedEx = Record.Exception(() => enumerator.Current);
            Assert.IsType<InvalidOperationException>(enumerationNotStartedEx);

            Assert.True(enumerator.MoveNext());
            Assert.Same(itemsSource, enumerator.Current);

            Assert.False(enumerator.MoveNext());
            var enumerationStoppedEx = Record.Exception(() => enumerator.Current);
            Assert.IsType<InvalidOperationException>(enumerationStoppedEx);
        }

        private void TestEnumeratorEquality(IEnumerator enumerator1, IEnumerator enumerator2)
        {
            bool couldEnum1MoveNext = enumerator1.MoveNext();
            bool couldEnum2MoveNext = enumerator2.MoveNext();
            Assert.Equal(couldEnum1MoveNext, couldEnum2MoveNext);

            if (couldEnum1MoveNext)
            {
                Assert.Equal(enumerator1.Current, enumerator2.Current);
                TestEnumeratorEquality(enumerator1, enumerator2);
            }
        }

        #endregion

        /// <summary>
        /// Creates an items source with the specified number of items in it.
        /// </summary>
        private int[] CreateIntItemsSource(int itemCount)
        {
            itemCount = Math.Max(0, itemCount);
            int[] itemsSource = new int[itemCount];
            for (int i = 0; i < itemCount; i++)
            {
                itemsSource[i] = i;
            }
            return itemsSource;
        }

        private ObservableCollection<int> CreateObservableIntItemsSource(int itemCount)
        {
            return new ObservableCollection<int>(
                CreateIntItemsSource(itemCount));
        }

        private object CreateNonEnumerableItemsSource()
        {
            // any object is acceptable. just give it a name for convenience.
            return new { Name = "Non Enumerable ItemsSource" };
        }

    }

}
