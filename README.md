# MCAJNLP
这是JNLP版本的Minecraft Applet

这里有源代码（只有JNLP可以修改）,请点击上面进行下载

这里是备用的下载链接，如果你不想为了下载速度慢请去官网

## 支持的JRE
Java 6 ~ Java 8（可64或32位）

如需要下载Java6或7，请复制链接至浏览器

Java6：https://www.oracle.com/java/technologies/javase-java-archive-javase6-downloads.html

Java7：https://www.oracle.com/java/technologies/javase/javase7-archive-downloads.html

## 如何启动？

Internet Explorer: https://218.89.171.137:55914/Tutorial.html

Firefox: https://218.89.171.137:55914/TutorialFirefox.html

Chrome: https://218.89.171.137:55914/TutorialChrome.html

Edge: https://218.89.171.137:55914/TutorialEdge.html

## 关于java.security.AccessControlException问题
请更改java.policy，一般是在C:\Program Files\Java\(你的Java版本)\lib\security内（如果你用的是32位的Java，则为C:\Program Files (x86)\Java\(你的Java版本)\lib\security）

在末尾加两条

`permission java.net.SocketPermission "*:*", "accept,connect,resolve";`

`permission java.security.AllPermission;`

