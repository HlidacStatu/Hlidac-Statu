//------------------------------------------------------------------------------
// <auto-generated />
//
// This file was automatically generated by SWIG (https://www.swig.org).
// Version 4.1.1
//
// Do not make changes to this file unless you know what you are doing - modify
// the SWIG interface file instead.
//------------------------------------------------------------------------------

namespace Ufal.UDPipe {

public class Sentences : global::System.IDisposable, global::System.Collections.IEnumerable, global::System.Collections.Generic.IEnumerable<Sentence>
 {
  private global::System.Runtime.InteropServices.HandleRef swigCPtr;
  protected bool swigCMemOwn;

  internal Sentences(global::System.IntPtr cPtr, bool cMemoryOwn) {
    swigCMemOwn = cMemoryOwn;
    swigCPtr = new global::System.Runtime.InteropServices.HandleRef(this, cPtr);
  }

  internal static global::System.Runtime.InteropServices.HandleRef getCPtr(Sentences obj) {
    return (obj == null) ? new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero) : obj.swigCPtr;
  }

  internal static global::System.Runtime.InteropServices.HandleRef swigRelease(Sentences obj) {
    if (obj != null) {
      if (!obj.swigCMemOwn)
        throw new global::System.ApplicationException("Cannot release ownership as memory is not owned");
      global::System.Runtime.InteropServices.HandleRef ptr = obj.swigCPtr;
      obj.swigCMemOwn = false;
      obj.Dispose();
      return ptr;
    } else {
      return new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
    }
  }

  ~Sentences() {
    Dispose(false);
  }

  public void Dispose() {
    Dispose(true);
    global::System.GC.SuppressFinalize(this);
  }

  protected virtual void Dispose(bool disposing) {
    lock(this) {
      if (swigCPtr.Handle != global::System.IntPtr.Zero) {
        if (swigCMemOwn) {
          swigCMemOwn = false;
          udpipe_csharpPINVOKE.delete_Sentences(swigCPtr);
        }
        swigCPtr = new global::System.Runtime.InteropServices.HandleRef(null, global::System.IntPtr.Zero);
      }
    }
  }

  public Sentences(global::System.Collections.IEnumerable c) : this() {
    if (c == null)
      throw new global::System.ArgumentNullException("c");
    foreach (Sentence element in c) {
      this.Add(element);
    }
  }

  public Sentences(global::System.Collections.Generic.IEnumerable<Sentence> c) : this() {
    if (c == null)
      throw new global::System.ArgumentNullException("c");
    foreach (Sentence element in c) {
      this.Add(element);
    }
  }

  public bool IsFixedSize {
    get {
      return false;
    }
  }

  public bool IsReadOnly {
    get {
      return false;
    }
  }

  public Sentence this[int index]  {
    get {
      return getitem(index);
    }
    set {
      setitem(index, value);
    }
  }

  public int Capacity {
    get {
      return (int)capacity();
    }
    set {
      if (value < 0 || (uint)value < size())
        throw new global::System.ArgumentOutOfRangeException("Capacity");
      reserve((uint)value);
    }
  }

  public int Count {
    get {
      return (int)size();
    }
  }

  public bool IsSynchronized {
    get {
      return false;
    }
  }

  public void CopyTo(Sentence[] array)
  {
    CopyTo(0, array, 0, this.Count);
  }

  public void CopyTo(Sentence[] array, int arrayIndex)
  {
    CopyTo(0, array, arrayIndex, this.Count);
  }

  public void CopyTo(int index, Sentence[] array, int arrayIndex, int count)
  {
    if (array == null)
      throw new global::System.ArgumentNullException("array");
    if (index < 0)
      throw new global::System.ArgumentOutOfRangeException("index", "Value is less than zero");
    if (arrayIndex < 0)
      throw new global::System.ArgumentOutOfRangeException("arrayIndex", "Value is less than zero");
    if (count < 0)
      throw new global::System.ArgumentOutOfRangeException("count", "Value is less than zero");
    if (array.Rank > 1)
      throw new global::System.ArgumentException("Multi dimensional array.", "array");
    if (index+count > this.Count || arrayIndex+count > array.Length)
      throw new global::System.ArgumentException("Number of elements to copy is too large.");
    for (int i=0; i<count; i++)
      array.SetValue(getitemcopy(index+i), arrayIndex+i);
  }

  public Sentence[] ToArray() {
    Sentence[] array = new Sentence[this.Count];
    this.CopyTo(array);
    return array;
  }

  global::System.Collections.Generic.IEnumerator<Sentence> global::System.Collections.Generic.IEnumerable<Sentence>.GetEnumerator() {
    return new SentencesEnumerator(this);
  }

  global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator() {
    return new SentencesEnumerator(this);
  }

  public SentencesEnumerator GetEnumerator() {
    return new SentencesEnumerator(this);
  }

  // Type-safe enumerator
  /// Note that the IEnumerator documentation requires an InvalidOperationException to be thrown
  /// whenever the collection is modified. This has been done for changes in the size of the
  /// collection but not when one of the elements of the collection is modified as it is a bit
  /// tricky to detect unmanaged code that modifies the collection under our feet.
  public sealed class SentencesEnumerator : global::System.Collections.IEnumerator
    , global::System.Collections.Generic.IEnumerator<Sentence>
  {
    private Sentences collectionRef;
    private int currentIndex;
    private object currentObject;
    private int currentSize;

    public SentencesEnumerator(Sentences collection) {
      collectionRef = collection;
      currentIndex = -1;
      currentObject = null;
      currentSize = collectionRef.Count;
    }

    // Type-safe iterator Current
    public Sentence Current {
      get {
        if (currentIndex == -1)
          throw new global::System.InvalidOperationException("Enumeration not started.");
        if (currentIndex > currentSize - 1)
          throw new global::System.InvalidOperationException("Enumeration finished.");
        if (currentObject == null)
          throw new global::System.InvalidOperationException("Collection modified.");
        return (Sentence)currentObject;
      }
    }

    // Type-unsafe IEnumerator.Current
    object global::System.Collections.IEnumerator.Current {
      get {
        return Current;
      }
    }

    public bool MoveNext() {
      int size = collectionRef.Count;
      bool moveOkay = (currentIndex+1 < size) && (size == currentSize);
      if (moveOkay) {
        currentIndex++;
        currentObject = collectionRef[currentIndex];
      } else {
        currentObject = null;
      }
      return moveOkay;
    }

    public void Reset() {
      currentIndex = -1;
      currentObject = null;
      if (collectionRef.Count != currentSize) {
        throw new global::System.InvalidOperationException("Collection modified.");
      }
    }

    public void Dispose() {
        currentIndex = -1;
        currentObject = null;
    }
  }

  public void Clear() {
    udpipe_csharpPINVOKE.Sentences_Clear(swigCPtr);
  }

  public void Add(Sentence x) {
    udpipe_csharpPINVOKE.Sentences_Add(swigCPtr, Sentence.getCPtr(x));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  private uint size() {
    uint ret = udpipe_csharpPINVOKE.Sentences_size(swigCPtr);
    return ret;
  }

  private uint capacity() {
    uint ret = udpipe_csharpPINVOKE.Sentences_capacity(swigCPtr);
    return ret;
  }

  private void reserve(uint n) {
    udpipe_csharpPINVOKE.Sentences_reserve(swigCPtr, n);
  }

  public Sentences() : this(udpipe_csharpPINVOKE.new_Sentences__SWIG_0(), true) {
  }

  public Sentences(Sentences other) : this(udpipe_csharpPINVOKE.new_Sentences__SWIG_1(Sentences.getCPtr(other)), true) {
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public Sentences(int capacity) : this(udpipe_csharpPINVOKE.new_Sentences__SWIG_2(capacity), true) {
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  private Sentence getitemcopy(int index) {
    Sentence ret = new Sentence(udpipe_csharpPINVOKE.Sentences_getitemcopy(swigCPtr, index), true);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private Sentence getitem(int index) {
    Sentence ret = new Sentence(udpipe_csharpPINVOKE.Sentences_getitem(swigCPtr, index), false);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  private void setitem(int index, Sentence val) {
    udpipe_csharpPINVOKE.Sentences_setitem(swigCPtr, index, Sentence.getCPtr(val));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public void AddRange(Sentences values) {
    udpipe_csharpPINVOKE.Sentences_AddRange(swigCPtr, Sentences.getCPtr(values));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public Sentences GetRange(int index, int count) {
    global::System.IntPtr cPtr = udpipe_csharpPINVOKE.Sentences_GetRange(swigCPtr, index, count);
    Sentences ret = (cPtr == global::System.IntPtr.Zero) ? null : new Sentences(cPtr, true);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void Insert(int index, Sentence x) {
    udpipe_csharpPINVOKE.Sentences_Insert(swigCPtr, index, Sentence.getCPtr(x));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public void InsertRange(int index, Sentences values) {
    udpipe_csharpPINVOKE.Sentences_InsertRange(swigCPtr, index, Sentences.getCPtr(values));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public void RemoveAt(int index) {
    udpipe_csharpPINVOKE.Sentences_RemoveAt(swigCPtr, index);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public void RemoveRange(int index, int count) {
    udpipe_csharpPINVOKE.Sentences_RemoveRange(swigCPtr, index, count);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public static Sentences Repeat(Sentence value, int count) {
    global::System.IntPtr cPtr = udpipe_csharpPINVOKE.Sentences_Repeat(Sentence.getCPtr(value), count);
    Sentences ret = (cPtr == global::System.IntPtr.Zero) ? null : new Sentences(cPtr, true);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
    return ret;
  }

  public void Reverse() {
    udpipe_csharpPINVOKE.Sentences_Reverse__SWIG_0(swigCPtr);
  }

  public void Reverse(int index, int count) {
    udpipe_csharpPINVOKE.Sentences_Reverse__SWIG_1(swigCPtr, index, count);
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

  public void SetRange(int index, Sentences values) {
    udpipe_csharpPINVOKE.Sentences_SetRange(swigCPtr, index, Sentences.getCPtr(values));
    if (udpipe_csharpPINVOKE.SWIGPendingException.Pending) throw udpipe_csharpPINVOKE.SWIGPendingException.Retrieve();
  }

}

}
