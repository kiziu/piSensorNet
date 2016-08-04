SELECT 
	p.ID, m.Address AS Module, p.MessageID, f.Name AS Function, p.Number, p.State, p.Text, 
    p.Created, p.Received, p.Handled
FROM Packets p
	LEFT JOIN Modules m ON m.ID = p.ModuleID
    LEFT JOIN Functions f ON f.ID = p.FunctionID;