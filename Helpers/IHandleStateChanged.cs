namespace DBF.Helpers
{
    internal interface IHandleStateChanged
    {
        //void OnStateChanged(object sender, EventArgs e);
     
        void ToggleMaximize(bool maximize);
    }
}
