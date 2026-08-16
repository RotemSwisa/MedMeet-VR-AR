using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

public class DocumentModel : RealtimeModel
{
    private Vector3 _position;
    private Quaternion _rotation = Quaternion.identity;
    private Vector3 _scale = Vector3.one;
    private int _currentPage = 0;
   
    

    public Vector3 position
    {
        get { return _position; }
        set
        {
            if (_position != value)
            {
                _position = value;
                positionDidChange?.Invoke(this, value);
            }
        }
    }

    public Quaternion rotation
    {
        get { return _rotation; }
        set
        {
            if (_rotation != value)
            {
                _rotation = value;
                rotationDidChange?.Invoke(this, value);
            }
        }
    }

    public Vector3 scale
    {
        get { return _scale; }
        set
        {
            if (_scale != value)
            {
                _scale = value;
                scaleDidChange?.Invoke(this, value);
            }
        }
    }

    public int currentPage
    {
        get { return _currentPage; }
        set
        {
            if (_currentPage != value)
            {
                _currentPage = value;
                currentPageDidChange?.Invoke(this, value);
            }
        }
    }

    public delegate void PropertyChangedHandler<T>(DocumentModel model, T value);
    public event PropertyChangedHandler<Vector3> positionDidChange;
    public event PropertyChangedHandler<Quaternion> rotationDidChange;
    public event PropertyChangedHandler<Vector3> scaleDidChange;
    public event PropertyChangedHandler<int> currentPageDidChange;

    // הפונקציות הנדרשות על ידי Normcore
    protected override int WriteLength(StreamContext context)
    {
        return 0; // לא משתמשים בזה כרגע
    }

    protected override void Write(WriteStream stream, StreamContext context)
    {
        // לא משתמשים בזה כרגע
    }

    protected override void Read(ReadStream stream, StreamContext context)
    {
        // לא משתמשים בזה כרגע
    }
}