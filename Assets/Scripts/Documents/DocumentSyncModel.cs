using UnityEngine;
using Normal.Realtime;
using Normal.Realtime.Serialization;

public class DocumentSyncModel : RealtimeModel
{
    private string _documentData = "";
    private string _fileName = "";
    private bool _isPDF = false;

    public string documentData
    {
        get { return _documentData; }
        set
        {
            if (_documentData != value)
            {
                _documentData = value;
                documentDataDidChange?.Invoke(this, value);
            }
        }
    }

    public string fileName
    {
        get { return _fileName; }
        set { _fileName = value; }
    }

    public bool isPDF
    {
        get { return _isPDF; }
        set { _isPDF = value; }
    }

    public delegate void PropertyChangedHandler(DocumentSyncModel model, string value);
    public event PropertyChangedHandler documentDataDidChange;

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