   M S B A M L   `   `   `  ���� G  Cguardian_wpf, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null+clr-namespace:guardian_wpfguardian_wpf      guardian_wpf.App   W SPresentationCore, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=6.0.2.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     R NSystem.Xaml, Version=6.0.2.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     ,localclr-namespace:guardian_wpf       �� 	Resources     ScrollThumbs �� 
TargetType �� Property �� Value �� 
TargetType �Name ��Name �� CornerRadius �� Triggers	 P� Property
 P� Value �� 
TargetName  FontAwesomeSolid � RowDefinitions ]�Name ]� Thumb n�Name J� Opacity ]� IncreaseRepeatButton ��Name ]� DecreaseRepeatButton P� 
SourceName �� Angle��%
  &	        &	 O    '�� �    �� " n�.���  � �� �� " n�.R�� $
 Grid��.+��� $��Stretch=�$��Stretch=�$
��Auto��$
��Auto��
������ �� $ 
Rectangle1��$ 5��$��Stretch=�$��Stretch=�$
��Auto��$
��Auto��#��z�� P� 	 �@�Tag$
 
Horizontal��. ��� $ 
Rectangle1�� � ��$
 Auto���� $ 
Rectangle1�� � ��$ 7��*�\Xpack://application:,,,/fonts/font awesome 6 free-solid-900.otf#Font Awesome 6 Free Solid)��� " ��.���  �@��IsFlicksEnabled$ false����  � �� �	#FF4D0B77��  � ��
 ���� ��  � ��$ 8����  � �� �� " ��.R�� $ GridRoot��$��8��#��z�� �� $��0.00001* �.+�]� $ 
PART_Track��$��0��$
��true��$|�false�� n� $ Thumb��$��-2.5, 10, 0, 0q�$	 0.2Q�#��z��#���    �� $ PageUp��$��ScrollBar.PageDownCommand��$ 0Q�$|�false�� �� $ PageDown��$��ScrollBar.PageUpCommand��$ 0Q�$|�false�� P� $ Thumb��	 �@J�IsMouseOver$

 true��. ��� $ Thumb�� �@J�Opacity$ 1Q�P� $ Thumb��	 �@n�
IsDragging$

 true��P� 	 � {�$
 false��. ��� $ Thumb�� � y�$ 	Collapsed=�P� 	 �@��Orientation$
 
Horizontal=�. ��� $ GridRoot�� �@�LayoutTransform �� $	 -90Q��� $ 
PART_Track�� �@�LayoutTransform �� $	 -90Q���  � ��$
 Auto����  � ��$ 8���� $ Thumb�� �@�Tag$ 
Horizontal���� $ PageDown�� � ��$ ScrollBar.PageLeftCommand���� $ PageUp�� � ��$  ScrollBar.PageRightCommand��