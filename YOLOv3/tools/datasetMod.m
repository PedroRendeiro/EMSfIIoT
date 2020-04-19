close all
clearvars
clc

global next

f = figure;
f.WindowState = 'maximized';
c = uicontrol;
c.String = 'Go Back';
c.Callback = @plotButtonPushed;

fileID = fopen('../data/EMSfIIoT/train.txt','r');
fID = fopen('../data/EMSfIIoT/textfile.txt', 'a');

tline = fgetl(fileID);
while ischar(tline)
    next = true;
    fileDir = split(tline,' ');
    fileDir = strcat('..\', fileDir{1});
    im = imread(fileDir);
    
    imshow(im);
    answer = inputdlg('Insert number in counter');
    fprintf(fID, '%s', tline);
    for i = 1:strlength(answer{1})
        number = answer{1}(i);
        title(['Identify number ', number, ' position'])
        roi = drawrectangle;

        if ~next
            break
        end

        left = round(roi.Position(1));
        top = round(roi.Position(2));
        width = round(roi.Position(3));
        height = round(roi.Position(4));
        text(left+width/2, top-height/2, number, 'Color', 'Red', 'FontSize', 18)

        number = str2double(number);

        x_min = left;
        y_min = top;
        x_max = left + width;
        y_max = top + height;
        class_id = number;

        result = [x_min,y_min,x_max,y_max,class_id];
        fprintf(fID, ' %d,%d,%d,%d,%d', result);
    end
    if next
        fprintf(fID, '\n');
        title('Done!')
        pause(1)
    end
    
    tline = fgetl(fileID);
end
fclose(fileID);
fclose(fID);

function plotButtonPushed(src,event)
    global next
    next = false;
end