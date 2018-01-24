Visual Basic.NET（根据官方C#版转写）
====

17mon IP库解析代码VB.NET版

##基本用法
```vb

IP.EnableFileWatch = True '默认值为：false，如果为true将会检查ip库文件的变化自动reload数据

IP.Load("IP库本地绝对路径")

IP.Find("8.8.8.8") '返回字符串数组["GOOGLE","GOOGLE"]

```

目前只有.dat解析。