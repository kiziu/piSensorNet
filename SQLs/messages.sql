SELECT 
	m.ID, md.Address AS Module, m.ResultMessageID, f.Name AS Function, m.Text, 
    m.IsQuery, m.State, m.Created, m.Sent, m.ResponseReceived
FROM Messages m
	LEFT JOIN Modules md ON md.ID = m.ModuleID
	JOIN Functions f ON f.ID = m.FunctionID;