   M S B A M L   `   `   `  ����G  Cguardian_wpf, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:guardian_wpfguardian_wpf  5          guardian_wpf.App   5      W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5      R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      ,localclr-namespace:guardian_wpf     5        �� 	Resources  5         ScrollThumbs �� 
TargetType �� Property �� Value �� 
TargetType �Name ��Name �� CornerRadius �� Triggers	 P� Property
 P� Value �� 
TargetName  FontAwesomeSolid � RowDefinitions ]�Name ]� Thumb n�Name J� Opacity ]� IncreaseRepeatButton ��Name ]� DecreaseRepeatButton P� 
SourceName �� Angle��%�	  &	        &	 C    '�� �    �� 5   
   " n�6%   .�5      ��  � �� 5	      �� 5
      " n�6&   .R�5      � $
 Grid��6   .+�5      �� $��Stretch=�6(   $��Stretch=�6F   $
��Auto��6b   $
��Auto��6o   
������ 6}   �� 5      $ 
Rectangle1��6%   $ 5��69   $��Stretch=�6J   $��Stretch=�6h   $
��Auto��6�   $
��Auto��6�   #��z��6�   5       5      P� 5      	 �@�Tag$
 
Horizontal��65   . �5   "   �� $ 
Rectangle1��6)    � ��$
 Auto��6R   �� 5   "   $ 
Rectangle1��6)    � ��$ 7��6S   5      5      5      5      *�5   
   \Xpack://application:,,,/fonts/font awesome 6 free-solid-900.otf#Font Awesome 6 Free Solid)�6�   �� 5   
   " ��6+   .�5      ��  �@��IsFlicksEnabled$ false��67   �� 5       � �� �	#FF4D0B776+   �� 5        � ��
 ���� 6+   �� 5!       � ��$ 8��6&   �� 5"       � �� 5#      �� 5$      " ��6&   .R�5%      � $ GridRoot��6   $��8��61   #��z��6;    5&      �� 5'   "   $��0.00001* �60   .+�5)      ]� $ 
PART_Track��6$   $��0��68   $
��true��6E   $|�false��6`    5*   "   n� 5+   &   $ Thumb��6,   $��-2.5, 10, 0, 0q�6;   $	 0.2Q�6S   #��z��6a   #���   6�    5-   "   �� 5.   &   $ PageUp��63   $��ScrollBar.PageDownCommand��6C   $ 0Q�6g   $|�false��6s    50   "   �� 51   &   $ PageDown��63   $��ScrollBar.PageUpCommand��6E   $ 0Q�6g   $|�false��6s   53      54       55      P� 56      $ Thumb��6&   	 �@J�IsMouseOver$

 true��6P   . �57   "   �� $ Thumb��6)    �@J�Opacity$ 1Q�6O   58      P� 59      $ Thumb��6&   	 �@n�
IsDragging$

 true��6O   5:      P� 5;      	 � {�$
 false��6;   . �5<   "   �� $ Thumb��6)    � y�$ 	Collapsed=�6R   5=      P� 5>      	 �@��Orientation$
 
Horizontal=�6=   . �5?   "   �� $ GridRoot��6)    �@�LayoutTransform 5@   &   �� 5A   *   $	 -90Q�6:   5C   #   �� 5D   "   $ 
PART_Track��6)    �@�LayoutTransform 5E   &   �� 5F   *   $	 -90Q�6:   5H   #   �� 5I   "    � ��$
 Auto��6:   �� 5J   "    � ��$ 8��6;   �� 5K   "   $ Thumb��6)    �@�Tag$ 
Horizontal��6K   �� 5L   "   $ PageDown��6)    � ��$ ScrollBar.PageLeftCommand��6R   �� 5M   "   $ PageUp��6)    � ��$  ScrollBar.PageRightCommand��6P   5N      5P      5R      5S      5T      5U      