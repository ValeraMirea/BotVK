CREATE TABLE [dbo].[Users_Data]
(
	[ID пользователя ВК] INT NOT NULL , 
    [Имя пользователя ВК (на основе ид)] CHAR(30) NOT NULL , 
    [Райтинг пользователя] INT NULL DEFAULT 0, 
    [Предупреждения] INT NULL DEFAULT 0, 
    [Бан] BIT NOT NULL DEFAULT 0 , 
    PRIMARY KEY ([ID пользователя ВК]) 
)
